using Microsoft.EntityFrameworkCore;
using RentalManagementSystem.Domain.Entities;
using RentalManagementSystem.Domain.Enums;
using Xunit;

namespace RentalManagementSystem.Tests;

public class RepositoryTests : TestBase
{
    [Fact]
    public async Task XoaMem_BiLocKhoiQuery()
    {
        var room = await AddRoomAsync("RG01");
        Uow.Rooms.Remove(room);
        await Uow.SaveChangesAsync();
        Assert.Null(await Uow.Rooms.FirstOrDefaultAsync(r => r.Id == room.Id));
        Assert.Empty(await Uow.Rooms.FindAsync(r => r.Id == room.Id));
    }

    [Fact]
    public async Task CountAsync_CoDieuKien_DemDung()
    {
        await AddRoomAsync("RG02");
        var room = await AddRoomAsync("RG03");
        room.Status = RoomStatus.Occupied; Uow.Rooms.Update(room); await Uow.SaveChangesAsync();
        Assert.Equal(1, await Uow.Rooms.CountAsync(r => r.Status == RoomStatus.Occupied));
    }

    [Fact]
    public async Task AddRange_ThemNhieuCungLuc()
    {
        await Uow.Tenants.AddRangeAsync(new[]
        {
            new Tenant { FullName = "T1", Phone = "1" },
            new Tenant { FullName = "T2", Phone = "2" },
        });
        await Uow.SaveChangesAsync();
        Assert.True((await Uow.Tenants.GetAllAsync()).Count() >= 2);
    }

    [Fact]
    public async Task GetById_KhongTonTai_TraVeNull()
        => Assert.Null(await Uow.Rooms.GetByIdAsync(999999));
}

public class DomainComputedTests : TestBase
{
    [Fact]
    public void UtilityReading_TinhTienDienNuocVaSanLuong()
    {
        var r = new UtilityReading
        {
            ElectricOld = 100, ElectricNew = 250, ElectricPrice = 3500,
            WaterOld = 10, WaterNew = 25, WaterPrice = 15000
        };
        Assert.Equal(150, r.ElectricUsage);
        Assert.Equal(15, r.WaterUsage);
        Assert.Equal(525_000, r.ElectricAmount);   // 150 * 3500
        Assert.Equal(225_000, r.WaterAmount);       // 15 * 15000
        Assert.Equal(750_000, r.TotalAmount);
    }

    [Fact]
    public void Invoice_OutstandingAmount_TinhDung()
    {
        var inv = new Invoice { TotalAmount = 1_000_000, PaidAmount = 300_000 };
        Assert.Equal(700_000, inv.OutstandingAmount);
    }
}

public class ContractExtendedTests : TestBase
{
    [Fact]
    public async Task GiaHan_KeoDaiNgayKetThuc()
    {
        var room = await AddRoomAsync("CE01");
        var t = await AddTenantAsync("KH CE01");
        var c = await AddActiveContractAsync(room.Id, t.Id);
        var newEnd = DateTime.Today.AddYears(1);
        Assert.True(await NewContractService().RenewAsync(c.Id, newEnd));
        var reloaded = await Uow.Contracts.GetByIdAsync(c.Id);
        Assert.Equal(newEnd.Date, reloaded!.EndDate.Date);
        Assert.Equal(ContractStatus.Active, reloaded.Status);
    }

    [Fact]
    public async Task SinhSoHopDong_DungDinhDang_VaTang()
    {
        var svc = NewContractService();
        var no1 = await svc.GenerateContractNoAsync();
        Assert.StartsWith("HĐ" + DateTime.Now.ToString("yyyyMM"), no1);

        var room = await AddRoomAsync("CE02");
        var t = await AddTenantAsync("KH CE02");
        await AddActiveContractAsync(room.Id, t.Id);
        var no2 = await svc.GenerateContractNoAsync();
        Assert.NotEqual(no1, no2);
    }

    [Fact]
    public async Task NhuongPhong_KhongGiuCoc_HoanCocCuVaThuCocMoi()
    {
        var room = await AddRoomAsync("CE03");
        var t1 = await AddTenantAsync("Cũ");
        var t2 = await AddTenantAsync("Mới");
        var c = await AddActiveContractAsync(room.Id, t1.Id, deposit: 3_000_000);

        Assert.True(await NewContractService().TransferAsync(c.Id, t2.Id, newDeposit: 4_000_000, keepDeposit: false));

        var refunds = await Uow.IncomeExpenses.FindAsync(ie => ie.IsDeposit && ie.Type == TransactionType.Expense);
        Assert.Contains(refunds, r => r.Amount == 3_000_000);
        var newDeposits = await Uow.IncomeExpenses.FindAsync(ie => ie.IsDeposit && ie.Type == TransactionType.Income && ie.Amount == 4_000_000);
        Assert.NotEmpty(newDeposits);
    }

    [Fact]
    public async Task NhuongPhong_ChoCungKhach_BiTuChoi()
    {
        var room = await AddRoomAsync("CE04");
        var t = await AddTenantAsync("KH CE04");
        var c = await AddActiveContractAsync(room.Id, t.Id);
        Assert.False(await NewContractService().TransferAsync(c.Id, t.Id, 0, true));
    }

    [Fact]
    public async Task CongNoTheoHopDong_BoQuaHoaDonDaThu()
    {
        var room = await AddRoomAsync("CE05");
        var t = await AddTenantAsync("KH CE05");
        var c = await AddActiveContractAsync(room.Id, t.Id);
        var inv = NewInvoiceService();
        await inv.CreateAsync(new Invoice { RoomId = room.Id, TenantId = t.Id, Month = 5, Year = 2026, RentAmount = 500_000, Status = InvoiceStatus.Unpaid });
        await inv.CreateAsync(new Invoice { RoomId = room.Id, TenantId = t.Id, Month = 4, Year = 2026, RentAmount = 300_000, Status = InvoiceStatus.Paid, PaidAmount = 300_000 });

        Assert.Equal(500_000, await NewContractService().GetOutstandingByContractAsync(c.Id));
    }

    [Fact]
    public async Task ThanhLy_HopDongKhongTonTai_ThatBai()
        => Assert.False(await NewContractService().TerminateAsync(999999));
}

public class DashboardExtendedTests : TestBase
{
    [Fact]
    public async Task DoanhThu12Thang_TraVe12Dong()
    {
        var data = (await NewDashboardService().GetMonthlyRevenueAsync(2026)).ToList();
        Assert.Equal(12, data.Count);
    }

    [Fact]
    public async Task TyLeLapDay_TheoKhu()
    {
        var room = await AddRoomAsync("DB01");
        var t = await AddTenantAsync("KH DB01");
        await AddActiveContractAsync(room.Id, t.Id);
        var occ = (await NewDashboardService().GetOccupancyRateAsync()).ToList();
        Assert.NotEmpty(occ);
        Assert.True(occ.Sum(o => o.Occupied) >= 1);
    }

    [Fact]
    public async Task ThongKe_TuDongChuyenHopDongQuaHan()
    {
        var room = await AddRoomAsync("DB02");
        var t = await AddTenantAsync("KH DB02");
        var c = await AddActiveContractAsync(room.Id, t.Id);
        c.EndDate = DateTime.Today.AddDays(-1); Uow.Contracts.Update(c); await Uow.SaveChangesAsync();

        await NewDashboardService().GetStatsAsync(); // tự bảo trì trạng thái
        var reloaded = await Uow.Contracts.GetByIdAsync(c.Id);
        Assert.Equal(ContractStatus.Expired, reloaded!.Status);
    }

    [Fact]
    public async Task CanhBao_TienQuaHan_DungTong()
    {
        var room = await AddRoomAsync("DB03");
        var t = await AddTenantAsync("KH DB03");
        await NewInvoiceService().CreateAsync(new Invoice
        { RoomId = room.Id, TenantId = t.Id, Month = 5, Year = 2026, RentAmount = 800_000, Status = InvoiceStatus.Overdue });

        var alerts = await NewDashboardService().GetAlertsAsync();
        Assert.True(alerts.OverdueAmount >= 800_000);
    }
}
