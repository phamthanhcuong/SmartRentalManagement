using System.Linq.Expressions;
using RentalManagementSystem.Domain.Entities;

namespace RentalManagementSystem.Application.Interfaces;

public interface IRepository<T> where T : BaseEntity
{
    Task<T?> GetByIdAsync(int id);
    Task<IEnumerable<T>> GetAllAsync();
    Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate);
    Task<T?> FirstOrDefaultAsync(Expression<Func<T, bool>> predicate);
    Task AddAsync(T entity);
    Task AddRangeAsync(IEnumerable<T> entities);
    void Update(T entity);
    void Remove(T entity);
    Task<int> CountAsync(Expression<Func<T, bool>>? predicate = null);
    IQueryable<T> Query();
}

public interface IUnitOfWork : IDisposable
{
    IRepository<User> Users { get; }
    IRepository<RentalArea> RentalAreas { get; }
    IRepository<Room> Rooms { get; }
    IRepository<Tenant> Tenants { get; }
    IRepository<Contract> Contracts { get; }
    IRepository<UtilityReading> UtilityReadings { get; }
    IRepository<Domain.Entities.Service> Services { get; }
    IRepository<Invoice> Invoices { get; }
    IRepository<InvoiceDetail> InvoiceDetails { get; }
    IRepository<IncomeExpense> IncomeExpenses { get; }
    IRepository<Asset> Assets { get; }
    IRepository<RoomAsset> RoomAssets { get; }
    IRepository<ContractSubscription> ContractSubscriptions { get; }
    IRepository<Vehicle> Vehicles { get; }
    IRepository<SystemSetting> SystemSettings { get; }
    IRepository<AuditLog> AuditLogs { get; }
    Task<int> SaveChangesAsync();
}
