using RentalManagementSystem.Application.Interfaces;
using RentalManagementSystem.Domain.Entities;
using RentalManagementSystem.Infrastructure.Data;

namespace RentalManagementSystem.Infrastructure.Repositories;

public class UnitOfWork : IUnitOfWork
{
    private readonly AppDbContext _context;
    private bool _disposed;

    public UnitOfWork(AppDbContext context)
    {
        _context = context;
        Users = new Repository<User>(context);
        RentalAreas = new Repository<RentalArea>(context);
        Rooms = new Repository<Room>(context);
        Tenants = new Repository<Tenant>(context);
        Contracts = new Repository<Contract>(context);
        UtilityReadings = new Repository<UtilityReading>(context);
        Services = new Repository<Domain.Entities.Service>(context);
        Invoices = new Repository<Invoice>(context);
        InvoiceDetails = new Repository<InvoiceDetail>(context);
        IncomeExpenses = new Repository<IncomeExpense>(context);
        Assets = new Repository<Asset>(context);
        RoomAssets = new Repository<RoomAsset>(context);
        ContractSubscriptions = new Repository<ContractSubscription>(context);
        Vehicles = new Repository<Vehicle>(context);
        SystemSettings = new Repository<SystemSetting>(context);
        AuditLogs = new Repository<AuditLog>(context);
    }

    public IRepository<User> Users { get; }
    public IRepository<RentalArea> RentalAreas { get; }
    public IRepository<Room> Rooms { get; }
    public IRepository<Tenant> Tenants { get; }
    public IRepository<Contract> Contracts { get; }
    public IRepository<UtilityReading> UtilityReadings { get; }
    public IRepository<Domain.Entities.Service> Services { get; }
    public IRepository<Invoice> Invoices { get; }
    public IRepository<InvoiceDetail> InvoiceDetails { get; }
    public IRepository<IncomeExpense> IncomeExpenses { get; }
    public IRepository<Asset> Assets { get; }
    public IRepository<RoomAsset> RoomAssets { get; }
    public IRepository<ContractSubscription> ContractSubscriptions { get; }
    public IRepository<Vehicle> Vehicles { get; }
    public IRepository<SystemSetting> SystemSettings { get; }
    public IRepository<AuditLog> AuditLogs { get; }

    public async Task<int> SaveChangesAsync() => await _context.SaveChangesAsync();

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed && disposing)
            _context.Dispose();
        _disposed = true;
    }
}
