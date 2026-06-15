using Microsoft.EntityFrameworkCore;
using RentalManagementSystem.Domain.Entities;
using RentalManagementSystem.Domain.Enums;
using Xunit;

namespace RentalManagementSystem.Tests;

public class InvoiceServiceTests : TestBase
{
    private async Task<(Room room, Contract contract)> SetupOccupiedRoomAsync(string code, decimal rent = 3_000_000)
    {
        var room = await AddRoomAsync(code, price: rent);
        var t = await AddTenantAsync("Khach " + code);
        var c = await AddActiveContractAsync(room.Id, t.Id, rent: rent);
        return (room, c);
    }

    [Fact]
    public async Task TaoHoaDonThang_CongDuTienThuePlusDienNuocPlusDichVu()
    {
        var (room, contract) = await SetupOccupiedRoomAsync("HD01", rent: 3_000_000);

        // Chỉ số điện nước: điện 100 số x 3500, nước 10 x 15000
        await NewUtilityService().CreateAsync(new UtilityReading
        {
            RoomId = room.Id, Month = 6, Year = 2026,
            ElectricOld = 0, ElectricNew = 100, WaterOld = 0, WaterNew = 10,
            ElectricPrice = 3500, WaterPrice = 15000
        });

        // Dịch vụ định kỳ: 100.000đ
        await Uow.ContractSubscriptions.AddAsync(new ContractSubscription
        { ContractId = contract.Id, ServiceId = 3, Quantity = 1, UnitPrice = 100_000 });
        await Uow.SaveChangesAsync();

        await NewInvoiceService().GenerateMonthlyInvoicesAsync(6, 2026);

        var inv = await Uow.Invoices.FirstOrDefaultAsync(i => i.RoomId == room.Id && i.Month == 6 && i.Year == 2026);
        Assert.NotNull(inv);
        Assert.Equal(3_000_000, inv!.RentAmount);
        Assert.Equal(350_000, inv.ElectricAmount);
        Assert.Equal(150_000, inv.WaterAmount);
        Assert.Equal(100_000, inv.ServiceAmount);
        Assert.Equal(3_600_000, inv.TotalAmount);
    }

    [Fact]
    public async Task ThuTien_TraMotPhanRoiTraDu_TrangThaiDungThuTu()
    {
        var (room, _) = await SetupOccupiedRoomAsync("HD02", rent: 1_000_000);
        await NewInvoiceService().GenerateMonthlyInvoicesAsync(6, 2026);
        var inv = await Uow.Invoices.FirstOrDefaultAsync(i => i.RoomId == room.Id);
        var svc = NewInvoiceService();

        Assert.True(await svc.PayAsync(inv!.Id, 400_000));
        var afterPartial = await Uow.Invoices.GetByIdAsync(inv.Id);
        Assert.Equal(InvoiceStatus.PartiallyPaid, afterPartial!.Status);

        Assert.True(await svc.PayAsync(inv.Id, 600_000));
        var afterFull = await Uow.Invoices.GetByIdAsync(inv.Id);
        Assert.Equal(InvoiceStatus.Paid, afterFull!.Status);
        Assert.Equal(1_000_000, afterFull.PaidAmount);
    }

    [Fact]
    public async Task ThuTien_KhongChoThuVuotSoConNo()
    {
        var (room, _) = await SetupOccupiedRoomAsync("HD03", rent: 1_000_000);
        await NewInvoiceService().GenerateMonthlyInvoicesAsync(6, 2026);
        var inv = await Uow.Invoices.FirstOrDefaultAsync(i => i.RoomId == room.Id);
        var svc = NewInvoiceService();

        await svc.PayAsync(inv!.Id, 5_000_000); // trả dư
        var after = await Uow.Invoices.GetByIdAsync(inv.Id);
        Assert.Equal(after!.TotalAmount, after.PaidAmount); // không vượt
    }

    [Fact]
    public async Task SinhSoHoaDon_KhongTrungSauKhiXoa()
    {
        var room = await AddRoomAsync("HD04");
        var t = await AddTenantAsync("K HD04");
        var svc = NewInvoiceService();

        var inv1 = new Invoice { RoomId = room.Id, TenantId = t.Id, Month = 6, Year = 2026, RentAmount = 100_000 };
        await svc.CreateAsync(inv1);
        var no1 = inv1.InvoiceNo;

        await svc.DeleteAsync(inv1.Id); // xóa mềm

        var inv2 = new Invoice { RoomId = room.Id, TenantId = t.Id, Month = 6, Year = 2026, RentAmount = 100_000 };
        await svc.CreateAsync(inv2);
        Assert.NotEqual(no1, inv2.InvoiceNo); // số mới không đụng số đã xóa
    }

    [Fact]
    public async Task TaoHoaDon_LamTronVND()
    {
        var room = await AddRoomAsync("HD05");
        var t = await AddTenantAsync("K HD05");
        var svc = NewInvoiceService();
        var inv = new Invoice { RoomId = room.Id, TenantId = t.Id, Month = 6, Year = 2026, RentAmount = 100_000.6m };
        await svc.CreateAsync(inv);
        Assert.Equal(Math.Round(inv.TotalAmount), inv.TotalAmount); // không còn số lẻ
    }

    [Fact]
    public async Task TaoHoaDonThang_CongNoKyTruoc_VaPhiTreHan()
    {
        var (room, _) = await SetupOccupiedRoomAsync("HD06", rent: 1_000_000);
        var svc = NewInvoiceService();

        // Hóa đơn kỳ trước (tháng 5) chưa thu 1.000.000
        await svc.CreateAsync(new Invoice { RoomId = room.Id, TenantId = (await Uow.Tenants.GetAllAsync()).First().Id, Month = 5, Year = 2026, RentAmount = 1_000_000, Status = InvoiceStatus.Unpaid });

        // Bật phí trễ hạn 10%
        var settings = NewSettings();
        var s = await settings.GetAsync();
        s.LateFeePercent = 10;
        await settings.UpdateAsync(s);

        await svc.GenerateMonthlyInvoicesAsync(6, 2026);
        var inv = await Uow.Invoices.FirstOrDefaultAsync(i => i.RoomId == room.Id && i.Month == 6 && i.Year == 2026);
        Assert.NotNull(inv);
        Assert.Equal(1_000_000, inv!.PreviousDebt);            // nợ kỳ trước
        Assert.Equal(100_000, inv.ServiceAmount);              // phí trễ hạn 10% của 1.000.000
    }
}

public class IncomeExpenseServiceTests : TestBase
{
    [Fact]
    public async Task TongThu_TongChi_TinhDung()
    {
        var svc = NewIncomeExpenseService();
        await svc.CreateAsync(new IncomeExpense { Type = TransactionType.Income, Category = "Khác", Amount = 1_000_000, TransactionDate = new DateTime(2026, 6, 10) });
        await svc.CreateAsync(new IncomeExpense { Type = TransactionType.Expense, Category = "Sửa chữa", Amount = 400_000, TransactionDate = new DateTime(2026, 6, 12) });

        Assert.Equal(1_000_000, await svc.GetTotalIncomeAsync(6, 2026));
        Assert.Equal(400_000, await svc.GetTotalExpenseAsync(6, 2026));
        Assert.Equal(0, await svc.GetTotalIncomeAsync(7, 2026));
    }
}

public class DashboardServiceTests : TestBase
{
    [Fact]
    public async Task ThongKe_DemPhongVaTrangThai()
    {
        var r1 = await AddRoomAsync("D01");
        await AddRoomAsync("D02");
        var t = await AddTenantAsync("KH D");
        await AddActiveContractAsync(r1.Id, t.Id);

        var stats = await NewDashboardService().GetStatsAsync();
        Assert.Equal(2, stats.TotalRooms);
        Assert.Equal(1, stats.OccupiedRooms);
        Assert.Equal(1, stats.AvailableRooms);
    }

    [Fact]
    public async Task CanhBao_PhongChuaNhapChiSo_VaChuaCoHoaDon()
    {
        var r1 = await AddRoomAsync("D03");
        var t = await AddTenantAsync("KH D3");
        await AddActiveContractAsync(r1.Id, t.Id);

        var alerts = await NewDashboardService().GetAlertsAsync();
        Assert.True(alerts.RoomsMissingReading >= 1);
        Assert.True(alerts.RoomsNoInvoice >= 1);
    }
}

public class SystemSettingServiceTests : TestBase
{
    [Fact]
    public async Task LayCauHinh_TonTaiBanGhiSeed()
    {
        var s = await NewSettings().GetAsync();
        Assert.NotNull(s);
        Assert.Equal(1, s.Id);
    }

    [Fact]
    public async Task CapNhatCauHinh_LuuThanhCong()
    {
        var svc = NewSettings();
        var s = await svc.GetAsync();
        s.CompanyName = "Nhà trọ Minh Tâm";
        s.DefaultElectricPrice = 4000;
        Assert.True(await svc.UpdateAsync(s));

        var reloaded = await svc.GetAsync();
        Assert.Equal("Nhà trọ Minh Tâm", reloaded.CompanyName);
        Assert.Equal(4000, reloaded.DefaultElectricPrice);
    }
}
