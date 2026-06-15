using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using RentalManagementSystem.Application.Interfaces;
using RentalManagementSystem.Application.Services;
using RentalManagementSystem.Domain.Entities;
using RentalManagementSystem.Domain.Enums;
using RentalManagementSystem.Infrastructure.Data;
using RentalManagementSystem.Infrastructure.Repositories;

namespace RentalManagementSystem.Tests;

/// <summary>
/// Nền tảng test: mỗi test có 1 CSDL SQLite in-memory riêng (sạch, độc lập),
/// dựng schema thật từ model (gồm filtered unique index) + dữ liệu seed.
/// </summary>
public abstract class TestBase : IDisposable
{
    private readonly SqliteConnection _connection;
    protected readonly AppDbContext Db;
    protected readonly IUnitOfWork Uow;

    protected TestBase()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_connection)
            .Options;
        Db = new AppDbContext(options);
        Db.Database.EnsureCreated();
        Uow = new UnitOfWork(Db);
    }

    // ----- Fakes / helpers -----
    protected sealed class FakeCurrentUser : ICurrentUser { public string UserName => "tester"; }

    protected IAuditService NewAudit() => new AuditService(Uow, new FakeCurrentUser(), NullLogger<AuditService>.Instance);
    protected ISystemSettingService NewSettings() => new SystemSettingService(Uow, NullLogger<SystemSettingService>.Instance);
    protected UserService NewUserService() => new(Uow, NullLogger<UserService>.Instance);
    protected RoomService NewRoomService() => new(Uow, NullLogger<RoomService>.Instance);
    protected TenantService NewTenantService() => new(Uow, NullLogger<TenantService>.Instance);
    protected VehicleService NewVehicleService() => new(Uow, NullLogger<VehicleService>.Instance);
    protected UtilityService NewUtilityService() => new(Uow, NullLogger<UtilityService>.Instance);
    protected ContractService NewContractService() => new(Uow, NewAudit(), NullLogger<ContractService>.Instance);
    protected InvoiceService NewInvoiceService() => new(Uow, NewAudit(), NewSettings(), NullLogger<InvoiceService>.Instance);
    protected IncomeExpenseService NewIncomeExpenseService() => new(Uow, NullLogger<IncomeExpenseService>.Instance);
    protected DashboardService NewDashboardService() => new(Uow, NullLogger<DashboardService>.Instance);
    protected RentalAreaService NewRentalAreaService() => new(Uow, NullLogger<RentalAreaService>.Instance);
    protected AssetService NewAssetService() => new(Uow, NullLogger<AssetService>.Instance);
    protected ServiceService NewServiceService() => new(Uow, NullLogger<ServiceService>.Instance);
    protected ContractSubscriptionService NewSubscriptionService() => new(Uow, NullLogger<ContractSubscriptionService>.Instance);
    protected ReportService NewReportService() => new(Uow, NullLogger<ReportService>.Instance);

    /// <summary>Thêm phòng vào khu seed (Id=1).</summary>
    protected async Task<Room> AddRoomAsync(string code, decimal price = 3_000_000, decimal deposit = 3_000_000)
    {
        var room = new Room { RoomCode = code, RoomName = code, RentalAreaId = 1, Price = price, Deposit = deposit, Status = RoomStatus.Available, CreatedAt = DateTime.Now };
        await Uow.Rooms.AddAsync(room);
        await Uow.SaveChangesAsync();
        return room;
    }

    protected async Task<Tenant> AddTenantAsync(string name, string phone = "0900000000")
    {
        var t = new Tenant { FullName = name, Phone = phone, CreatedAt = DateTime.Now };
        await Uow.Tenants.AddAsync(t);
        await Uow.SaveChangesAsync();
        return t;
    }

    /// <summary>Tạo hợp đồng hiệu lực qua ContractService (đúng nghiệp vụ).</summary>
    protected async Task<Contract> AddActiveContractAsync(int roomId, int tenantId, decimal rent = 3_000_000, decimal deposit = 3_000_000, int occupants = 1)
    {
        var c = new Contract
        {
            TenantId = tenantId, RoomId = roomId,
            StartDate = DateTime.Today.AddMonths(-1), EndDate = DateTime.Today.AddMonths(6),
            MonthlyRent = rent, DepositAmount = deposit, OccupantCount = occupants,
            Status = ContractStatus.Active
        };
        await NewContractService().CreateAsync(c);
        return c;
    }

    public void Dispose()
    {
        Db.Dispose();
        _connection.Dispose();
    }
}
