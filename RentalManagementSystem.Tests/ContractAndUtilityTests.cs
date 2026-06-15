using Microsoft.EntityFrameworkCore;
using RentalManagementSystem.Domain.Entities;
using RentalManagementSystem.Domain.Enums;
using Xunit;

namespace RentalManagementSystem.Tests;

public class ContractServiceTests : TestBase
{
    [Fact]
    public async Task TaoHopDong_PhongChuyenSangDangThue_VaGhiThuCoc()
    {
        var room = await AddRoomAsync("P301");
        var t = await AddTenantAsync("Khach 1");
        await AddActiveContractAsync(room.Id, t.Id, deposit: 5_000_000);

        var updatedRoom = await Uow.Rooms.GetByIdAsync(room.Id);
        Assert.Equal(RoomStatus.Occupied, updatedRoom!.Status);

        var deposits = await Uow.IncomeExpenses.FindAsync(ie => ie.IsDeposit && ie.Type == TransactionType.Income);
        Assert.Contains(deposits, d => d.Amount == 5_000_000);
    }

    [Fact]
    public async Task TaoHopDong_PhongDaCoHopDongHieuLuc_BiChan()
    {
        var room = await AddRoomAsync("P302");
        var t1 = await AddTenantAsync("Khach A");
        var t2 = await AddTenantAsync("Khach B");
        await AddActiveContractAsync(room.Id, t1.Id);

        var svc = NewContractService();
        var ok = await svc.CreateAsync(new Contract
        {
            TenantId = t2.Id, RoomId = room.Id,
            StartDate = DateTime.Today, EndDate = DateTime.Today.AddMonths(6),
            MonthlyRent = 3_000_000, Status = ContractStatus.Active
        });
        Assert.False(ok); // chống double-booking
    }

    [Fact]
    public async Task TraPhong_HoanCoc_VaPhongVeTrong()
    {
        var room = await AddRoomAsync("P303");
        var t = await AddTenantAsync("Khach 2");
        var c = await AddActiveContractAsync(room.Id, t.Id, deposit: 4_000_000);

        var svc = NewContractService();
        Assert.True(await svc.CheckoutAsync(c.Id, depositDeduction: 1_000_000));

        var updatedRoom = await Uow.Rooms.GetByIdAsync(room.Id);
        Assert.Equal(RoomStatus.Available, updatedRoom!.Status);

        var refunds = await Uow.IncomeExpenses.FindAsync(ie => ie.IsDeposit && ie.Type == TransactionType.Expense);
        Assert.Contains(refunds, r => r.Amount == 3_000_000); // 4 triệu - 1 triệu khấu trừ
    }

    [Fact]
    public async Task NhuongPhong_TaoHopDongMoi_HopDongCuThanhLy()
    {
        var room = await AddRoomAsync("P304");
        var t1 = await AddTenantAsync("Khach cũ");
        var t2 = await AddTenantAsync("Khach mới");
        var c = await AddActiveContractAsync(room.Id, t1.Id);

        var svc = NewContractService();
        Assert.True(await svc.TransferAsync(c.Id, t2.Id, newDeposit: 0, keepDeposit: true));

        var old = await Uow.Contracts.GetByIdAsync(c.Id);
        Assert.Equal(ContractStatus.Terminated, old!.Status);

        var active = await svc.GetActiveByRoomAsync(room.Id);
        Assert.NotNull(active);
        Assert.Equal(t2.Id, active!.TenantId);
    }

    [Fact]
    public async Task HopDongQuaHan_TuDongChuyenHetHan()
    {
        var room = await AddRoomAsync("P305");
        var t = await AddTenantAsync("Khach 3");
        var c = await AddActiveContractAsync(room.Id, t.Id);
        c.EndDate = DateTime.Today.AddDays(-5);
        Uow.Contracts.Update(c);
        await Uow.SaveChangesAsync();

        var n = await NewContractService().UpdateExpiredStatusesAsync();
        Assert.True(n >= 1);
        var reloaded = await Uow.Contracts.GetByIdAsync(c.Id);
        Assert.Equal(ContractStatus.Expired, reloaded!.Status);
    }
}

public class UtilityServiceTests : TestBase
{
    [Fact]
    public async Task GhiChiSo_ChiSoMoiNhoHonCu_BiTuChoi()
    {
        var room = await AddRoomAsync("P401");
        var svc = NewUtilityService();
        var ok = await svc.CreateAsync(new UtilityReading
        {
            RoomId = room.Id, Month = 6, Year = 2026,
            ElectricOld = 100, ElectricNew = 50, // sai: mới < cũ
            WaterOld = 10, WaterNew = 20
        });
        Assert.False(ok);
    }

    [Fact]
    public async Task GhiChiSo_KySau_TuKeThuaChiSoCuoiKyTruoc()
    {
        var room = await AddRoomAsync("P402");
        var svc = NewUtilityService();
        await svc.CreateAsync(new UtilityReading { RoomId = room.Id, Month = 5, Year = 2026, ElectricOld = 0, ElectricNew = 100, WaterOld = 0, WaterNew = 30 });

        // Kỳ sau để trống chỉ số cũ -> tự lấy từ kỳ trước (100, 30)
        var prev = await svc.GetPreviousReadingAsync(room.Id, 6, 2026);
        Assert.NotNull(prev);
        Assert.Equal(100, prev!.ElectricNew);
    }

    [Fact]
    public async Task LuuHangLoat_BoQuaDongChuaNhap_LuuDongHopLe()
    {
        var r1 = await AddRoomAsync("P403");
        var r2 = await AddRoomAsync("P404");
        var svc = NewUtilityService();
        var readings = new[]
        {
            new UtilityReading { RoomId = r1.Id, ElectricOld = 0, ElectricNew = 50, WaterOld = 0, WaterNew = 10, ElectricPrice = 3500, WaterPrice = 15000 },
            new UtilityReading { RoomId = r2.Id, ElectricOld = 0, ElectricNew = 0, WaterOld = 0, WaterNew = 0 }, // chưa nhập -> bỏ qua
        };
        var saved = await svc.SaveBatchAsync(6, 2026, readings);
        Assert.Equal(1, saved);
    }
}
