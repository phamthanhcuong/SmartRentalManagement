using RentalManagementSystem.Domain.Entities;
using RentalManagementSystem.Domain.Enums;
using Xunit;

namespace RentalManagementSystem.Tests;

public class UserServiceExtendedTests : TestBase
{
    [Fact]
    public async Task TaoUser_TrungTenDangNhap_ThatBai()
    {
        var svc = NewUserService();
        Assert.False(await svc.CreateAsync(new User { Username = "admin", FullName = "Trùng" }, "x123456"));
    }

    [Fact]
    public async Task TaoUser_Moi_RoiDangNhapDuoc()
    {
        var svc = NewUserService();
        Assert.True(await svc.CreateAsync(new User { Username = "thuky", FullName = "Thư Ký", Role = UserRole.Staff, IsActive = true }, "Pass@123"));
        Assert.NotNull(await svc.LoginAsync("thuky", "Pass@123"));
    }

    [Fact]
    public async Task DangNhap_TaiKhoanBiKhoa_TraVeNull()
    {
        var svc = NewUserService();
        await svc.CreateAsync(new User { Username = "khoa", FullName = "Khóa", IsActive = false }, "Pass@123");
        Assert.Null(await svc.LoginAsync("khoa", "Pass@123"));
    }

    [Fact]
    public async Task DangNhap_CapNhatLanDangNhapCuoi()
    {
        var u = await NewUserService().LoginAsync("admin", "Admin@123");
        Assert.NotNull(u!.LastLoginAt);
    }

    [Fact]
    public async Task DoiMatKhau_UserKhongTonTai_ThatBai()
        => Assert.False(await NewUserService().ChangePasswordAsync(9999, "x", "y123456"));
}

public class RentalAreaServiceTests : TestBase
{
    [Fact]
    public async Task ThemKhu_TrungMaKhu_ThatBai()
    {
        var svc = NewRentalAreaService();
        Assert.True(await svc.CreateAsync(new RentalArea { AreaCode = "KV01", AreaName = "Khu 1" }));
        Assert.False(await svc.CreateAsync(new RentalArea { AreaCode = "KV01", AreaName = "Khu trùng" }));
    }

    [Fact]
    public async Task SuaKhu_LuuThanhCong()
    {
        var svc = NewRentalAreaService();
        await svc.CreateAsync(new RentalArea { AreaCode = "KV02", AreaName = "Khu 2" });
        var area = (await svc.GetAllAsync()).First(a => a.AreaCode == "KV02");
        area.AreaName = "Khu 2 mới";
        Assert.True(await svc.UpdateAsync(area));
        Assert.Equal("Khu 2 mới", (await svc.GetByIdAsync(area.Id))!.AreaName);
    }

    [Fact]
    public async Task XoaKhu_KhongConTrongDanhSach()
    {
        var svc = NewRentalAreaService();
        await svc.CreateAsync(new RentalArea { AreaCode = "KV03", AreaName = "Khu 3" });
        var area = (await svc.GetAllAsync()).First(a => a.AreaCode == "KV03");
        Assert.True(await svc.DeleteAsync(area.Id));
        Assert.DoesNotContain(await svc.GetAllAsync(), a => a.Id == area.Id);
    }
}

public class ServiceServiceTests : TestBase
{
    [Fact]
    public async Task SeedCo4DichVuMacDinh()
    {
        var list = await NewServiceService().GetAllAsync();
        Assert.True(list.Count() >= 4);
    }

    [Fact]
    public async Task ThemDichVu_RoiLay()
    {
        var svc = NewServiceService();
        await svc.CreateAsync(new Service { ServiceName = "Rác", ServiceType = ServiceType.Other, UnitPrice = 20000, IsActive = true });
        Assert.Contains(await svc.GetAllAsync(), s => s.ServiceName == "Rác");
    }
}

public class AssetServiceTests : TestBase
{
    [Fact]
    public async Task ThemTaiSan_RoiLay()
    {
        var svc = NewAssetService();
        Assert.True(await svc.CreateAsync(new Asset { AssetCode = "TS01", AssetName = "Máy lạnh", PurchasePrice = 7_000_000 }));
        Assert.Contains(await svc.GetAllAsync(), a => a.AssetCode == "TS01");
    }

    [Fact]
    public async Task XoaTaiSan_ThanhCong()
    {
        var svc = NewAssetService();
        await svc.CreateAsync(new Asset { AssetCode = "TS02", AssetName = "Tủ lạnh" });
        var a = (await svc.GetAllAsync()).First(x => x.AssetCode == "TS02");
        Assert.True(await svc.DeleteAsync(a.Id));
    }
}

public class ContractSubscriptionTests : TestBase
{
    [Fact]
    public async Task DangKyDichVu_TrungDichVu_ThatBai()
    {
        var room = await AddRoomAsync("SUB01");
        var t = await AddTenantAsync("KH Sub");
        var c = await AddActiveContractAsync(room.Id, t.Id);
        var svc = NewSubscriptionService();

        Assert.True(await svc.AddAsync(new ContractSubscription { ContractId = c.Id, ServiceId = 3, Quantity = 1, UnitPrice = 100000 }));
        Assert.False(await svc.AddAsync(new ContractSubscription { ContractId = c.Id, ServiceId = 3, Quantity = 1, UnitPrice = 100000 }));
    }

    [Fact]
    public async Task XoaDangKyDichVu_ThanhCong()
    {
        var room = await AddRoomAsync("SUB02");
        var t = await AddTenantAsync("KH Sub2");
        var c = await AddActiveContractAsync(room.Id, t.Id);
        var svc = NewSubscriptionService();
        await svc.AddAsync(new ContractSubscription { ContractId = c.Id, ServiceId = 4, Quantity = 1, UnitPrice = 50000 });
        var sub = (await svc.GetByContractAsync(c.Id)).First();
        Assert.True(await svc.RemoveAsync(sub.Id));
        Assert.Empty(await svc.GetByContractAsync(c.Id));
    }
}
