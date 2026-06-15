using Microsoft.EntityFrameworkCore;
using RentalManagementSystem.Domain.Entities;
using RentalManagementSystem.Domain.Enums;
using Xunit;

namespace RentalManagementSystem.Tests;

public class AdvancedInvoiceTests : TestBase
{
    private async Task<(Room, Contract)> OccupiedAsync(string code, decimal rent = 1_000_000, int occupants = 1)
    {
        var room = await AddRoomAsync(code, price: rent);
        var t = await AddTenantAsync("KH " + code);
        var c = await AddActiveContractAsync(room.Id, t.Id, rent: rent, occupants: occupants);
        return (room, c);
    }

    [Fact]
    public async Task TaoHoaDonThang_ChayHaiLan_KhongTaoTrung()
    {
        var (room, _) = await OccupiedAsync("AI01");
        var svc = NewInvoiceService();
        await svc.GenerateMonthlyInvoicesAsync(6, 2026);
        await svc.GenerateMonthlyInvoicesAsync(6, 2026); // chạy lại
        var count = await Uow.Invoices.CountAsync(i => i.RoomId == room.Id && i.Month == 6 && i.Year == 2026);
        Assert.Equal(1, count);
    }

    [Fact]
    public async Task TaoHoaDonThang_BoQuaPhongKhongCoHopDong()
    {
        await AddRoomAsync("AI02"); // phòng trống, không HĐ
        await NewInvoiceService().GenerateMonthlyInvoicesAsync(6, 2026);
        Assert.Equal(0, await Uow.Invoices.CountAsync());
    }

    [Fact]
    public async Task TaoHoaDonThang_NhieuPhong_TaoDuHoaDon()
    {
        await OccupiedAsync("AI03");
        await OccupiedAsync("AI04");
        await NewInvoiceService().GenerateMonthlyInvoicesAsync(6, 2026);
        Assert.Equal(2, await Uow.Invoices.CountAsync(i => i.Month == 6 && i.Year == 2026));
    }

    [Fact]
    public async Task PhiDichVuTheoDauNguoi_NhanVoiSoNguoiO()
    {
        var (room, c) = await OccupiedAsync("AI05", rent: 1_000_000, occupants: 3);
        await Uow.ContractSubscriptions.AddAsync(new ContractSubscription
        { ContractId = c.Id, ServiceId = 4, Quantity = 1, UnitPrice = 20_000, IsPerPerson = true });
        await Uow.SaveChangesAsync();

        await NewInvoiceService().GenerateMonthlyInvoicesAsync(6, 2026);
        var inv = await Uow.Invoices.FirstOrDefaultAsync(i => i.RoomId == room.Id);
        Assert.Equal(60_000, inv!.ServiceAmount); // 20.000 x 3 người
    }

    [Fact]
    public async Task HoaDon_CoChiTietTungKhoan()
    {
        var (room, _) = await OccupiedAsync("AI06");
        await NewInvoiceService().GenerateMonthlyInvoicesAsync(6, 2026);
        var inv = await Uow.Invoices.FirstOrDefaultAsync(i => i.RoomId == room.Id);
        var full = await NewInvoiceService().GetByIdAsync(inv!.Id);
        Assert.NotEmpty(full!.InvoiceDetails);
        Assert.Contains(full.InvoiceDetails, d => d.ItemName.Contains("thuê"));
    }

    [Fact]
    public async Task DanhDauQuaHan_HoaDonChuaThuQuaHan_ChuyenOverdue()
    {
        var room = await AddRoomAsync("AI07");
        var t = await AddTenantAsync("KH AI07");
        var svc = NewInvoiceService();
        await svc.CreateAsync(new Invoice
        {
            RoomId = room.Id, TenantId = t.Id, Month = 5, Year = 2026,
            RentAmount = 500_000, DueDate = DateTime.Today.AddDays(-3), Status = InvoiceStatus.Unpaid
        });
        var n = await svc.MarkOverdueAsync();
        Assert.True(n >= 1);
        var inv = await Uow.Invoices.FirstOrDefaultAsync(i => i.RoomId == room.Id);
        Assert.Equal(InvoiceStatus.Overdue, inv!.Status);
    }

    [Fact]
    public async Task TongChuaThu_GomChuaThuVaTraMotPhan()
    {
        var (room, _) = await OccupiedAsync("AI08", rent: 1_000_000);
        var svc = NewInvoiceService();
        await svc.GenerateMonthlyInvoicesAsync(6, 2026);
        var inv = await Uow.Invoices.FirstOrDefaultAsync(i => i.RoomId == room.Id);
        await svc.PayAsync(inv!.Id, 300_000); // còn nợ 700.000
        Assert.Equal(700_000, await svc.GetTotalUnpaidAsync());
    }

    [Fact]
    public async Task DoanhThuThang_TinhTheoTienDaThu()
    {
        var (room, _) = await OccupiedAsync("AI09", rent: 1_000_000);
        var svc = NewInvoiceService();
        await svc.GenerateMonthlyInvoicesAsync(6, 2026);
        var inv = await Uow.Invoices.FirstOrDefaultAsync(i => i.RoomId == room.Id);
        await svc.PayAsync(inv!.Id, 1_000_000);
        Assert.Equal(1_000_000, await svc.GetRevenueByMonthAsync(6, 2026));
    }

    [Fact]
    public async Task XoaHoaDon_XoaLuonButToanThuLienQuan()
    {
        var (room, _) = await OccupiedAsync("AI10", rent: 1_000_000);
        var svc = NewInvoiceService();
        await svc.GenerateMonthlyInvoicesAsync(6, 2026);
        var inv = await Uow.Invoices.FirstOrDefaultAsync(i => i.RoomId == room.Id);
        await svc.PayAsync(inv!.Id, 500_000); // tạo bút toán thu
        Assert.NotEmpty(await Uow.IncomeExpenses.FindAsync(ie => ie.InvoiceId == inv.Id));

        await svc.DeleteAsync(inv.Id);
        Assert.Empty(await Uow.IncomeExpenses.FindAsync(ie => ie.InvoiceId == inv.Id));
    }

    [Fact]
    public async Task ThuTien_HoaDonDaHuy_BiTuChoi()
    {
        var room = await AddRoomAsync("AI11");
        var t = await AddTenantAsync("KH AI11");
        var svc = NewInvoiceService();
        var inv = new Invoice { RoomId = room.Id, TenantId = t.Id, Month = 6, Year = 2026, RentAmount = 100_000, Status = InvoiceStatus.Cancelled };
        await svc.CreateAsync(inv);
        // ép trạng thái huỷ (CreateAsync không đổi status)
        inv.Status = InvoiceStatus.Cancelled; Uow.Invoices.Update(inv); await Uow.SaveChangesAsync();
        Assert.False(await svc.PayAsync(inv.Id, 50_000));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-100)]
    public async Task ThuTien_SoTienKhongHopLe_BiTuChoi(decimal amount)
    {
        var (room, _) = await OccupiedAsync("AI12" + amount, rent: 1_000_000);
        var svc = NewInvoiceService();
        await svc.GenerateMonthlyInvoicesAsync(6, 2026);
        var inv = await Uow.Invoices.FirstOrDefaultAsync(i => i.RoomId == room.Id);
        Assert.False(await svc.PayAsync(inv!.Id, amount));
    }

    [Fact]
    public async Task SoHoaDon_DungDinhDang_HD_yyyyMM()
    {
        var no = await NewInvoiceService().GenerateInvoiceNoAsync();
        Assert.StartsWith("HD" + DateTime.Now.ToString("yyyyMM"), no);
        Assert.Equal(12, no.Length); // HD + 6 số tháng + 4 số thứ tự
    }
}

public class ReportServiceTests : TestBase
{
    [Fact]
    public async Task XuatBaoCaoDoanhThu_TaoFileExcel()
    {
        var path = Path.Combine(Path.GetTempPath(), $"rev_{Guid.NewGuid():N}.xlsx");
        await NewReportService().ExportRevenueToExcelAsync(6, 2026, path);
        Assert.True(File.Exists(path) && new FileInfo(path).Length > 0);
        File.Delete(path);
    }

    [Fact]
    public async Task XuatBaoCaoCongNo_TaoFileExcel()
    {
        var path = Path.Combine(Path.GetTempPath(), $"debt_{Guid.NewGuid():N}.xlsx");
        await NewReportService().ExportDebtToExcelAsync(path);
        Assert.True(File.Exists(path) && new FileInfo(path).Length > 0);
        File.Delete(path);
    }

    [Fact]
    public async Task InHoaDon_TaoFileExcel()
    {
        var room = await AddRoomAsync("RP01");
        var t = await AddTenantAsync("KH RP01");
        var svc = NewInvoiceService();
        var inv = new Invoice { RoomId = room.Id, TenantId = t.Id, Month = 6, Year = 2026, RentAmount = 1_000_000 };
        await svc.CreateAsync(inv);

        var path = Path.Combine(Path.GetTempPath(), $"inv_{Guid.NewGuid():N}.xlsx");
        await NewReportService().PrintInvoiceAsync(inv.Id, path);
        Assert.True(File.Exists(path) && new FileInfo(path).Length > 0);
        File.Delete(path);
    }
}

public class AuditServiceTests : TestBase
{
    [Fact]
    public async Task GhiNhatKy_LuuDungNguoiDung()
    {
        var audit = NewAudit();
        await audit.LogAsync("Thử nghiệm", "Test", "X1", "chi tiết");
        var logs = (await audit.GetRecentAsync()).ToList();
        Assert.Single(logs);
        Assert.Equal("tester", logs[0].UserName);
        Assert.Equal("Thử nghiệm", logs[0].Action);
    }

    [Fact]
    public async Task TaoHopDong_TuDongGhiNhatKy()
    {
        var room = await AddRoomAsync("AU01");
        var t = await AddTenantAsync("KH AU01");
        await AddActiveContractAsync(room.Id, t.Id);
        var logs = await NewAudit().GetRecentAsync();
        Assert.Contains(logs, l => l.Action == "Tạo hợp đồng");
    }

    [Fact]
    public async Task NhatKy_SapXepMoiNhatTruoc()
    {
        var audit = NewAudit();
        await audit.LogAsync("A", "T");
        await Task.Delay(5);
        await audit.LogAsync("B", "T");
        var logs = (await audit.GetRecentAsync()).ToList();
        Assert.Equal("B", logs[0].Action);
    }
}
