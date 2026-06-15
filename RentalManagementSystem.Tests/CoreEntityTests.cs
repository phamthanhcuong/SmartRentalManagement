using RentalManagementSystem.Domain.Entities;
using Xunit;

namespace RentalManagementSystem.Tests;

public class UserServiceTests : TestBase
{
    [Fact]
    public async Task Login_DungTaiKhoanMacDinh_TraVeUser()
    {
        var svc = NewUserService();
        var user = await svc.LoginAsync("admin", "Admin@123");
        Assert.NotNull(user);
        Assert.Equal("admin", user!.Username);
    }

    [Fact]
    public async Task Login_SaiMatKhau_TraVeNull()
    {
        var svc = NewUserService();
        Assert.Null(await svc.LoginAsync("admin", "sai-mat-khau"));
    }

    [Fact]
    public async Task DoiMatKhau_RoiDangNhapBangMatKhauMoi_ThanhCong()
    {
        var svc = NewUserService();
        var ok = await svc.ChangePasswordAsync(1, "Admin@123", "MatKhauMoi@1");
        Assert.True(ok);
        Assert.Null(await svc.LoginAsync("admin", "Admin@123"));
        Assert.NotNull(await svc.LoginAsync("admin", "MatKhauMoi@1"));
    }

    [Fact]
    public async Task DoiMatKhau_SaiMatKhauCu_ThatBai()
    {
        var svc = NewUserService();
        Assert.False(await svc.ChangePasswordAsync(1, "sai", "MatKhauMoi@1"));
    }
}

public class RoomServiceTests : TestBase
{
    [Fact]
    public async Task ThemPhong_RoiLayDanhSach_CoPhongMoi()
    {
        await AddRoomAsync("P101");
        var svc = NewRoomService();
        var rooms = await svc.GetAllAsync();
        Assert.Contains(rooms, r => r.RoomCode == "P101");
    }

    [Fact]
    public async Task XoaPhong_LaXoaMem_KhongConTrongDanhSach()
    {
        var room = await AddRoomAsync("P102");
        var svc = NewRoomService();
        Assert.True(await svc.DeleteAsync(room.Id));
        var rooms = await svc.GetAllAsync();
        Assert.DoesNotContain(rooms, r => r.Id == room.Id);
    }

    [Fact]
    public async Task TimKiem_TheoMaPhong_TraVeDung()
    {
        await AddRoomAsync("A999");
        await AddRoomAsync("B111");
        var svc = NewRoomService();
        var kq = await svc.SearchAsync("A99");
        Assert.Single(kq);
        Assert.Equal("A999", kq.First().RoomCode);
    }
}

public class VehicleServiceTests : TestBase
{
    [Fact]
    public async Task ThemXe_TrungBienSoDangHoatDong_ThatBai()
    {
        var t = await AddTenantAsync("Nguyen Van A");
        var svc = NewVehicleService();
        Assert.True(await svc.AddAsync(new Vehicle { TenantId = t.Id, LicensePlate = "59A-12345" }));
        Assert.False(await svc.AddAsync(new Vehicle { TenantId = t.Id, LicensePlate = "59a-12345" })); // trùng (chuẩn hóa hoa)
    }

    [Fact]
    public async Task TraCuuXe_GanDungChuVaPhong()
    {
        var room = await AddRoomAsync("P201");
        var t = await AddTenantAsync("Tran Thi B", "0911222333");
        await AddActiveContractAsync(room.Id, t.Id);
        await NewVehicleService().AddAsync(new Vehicle { TenantId = t.Id, LicensePlate = "60B1-555.66" });

        var kq = (await NewVehicleService().SearchAsync("60B1")).ToList();
        Assert.Single(kq);
        Assert.Equal("Tran Thi B", kq[0].OwnerName);
        Assert.Equal("P201", kq[0].RoomCode);
    }
}

public class TenantServiceTests : TestBase
{
    [Fact]
    public async Task ThemKhach_RoiTimKiem_TheoSDT()
    {
        var svc = NewTenantService();
        await svc.CreateAsync(new Tenant { FullName = "Le Van C", Phone = "0987654321" });
        var kq = await svc.SearchAsync("098765");
        Assert.Contains(kq, t => t.FullName == "Le Van C");
    }
}
