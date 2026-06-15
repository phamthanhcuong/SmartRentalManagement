using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RentalManagementSystem.Application.Interfaces;
using RentalManagementSystem.Domain.Entities;

namespace RentalManagementSystem.Application.Services;

public class TenantService : ITenantService
{
    private readonly IUnitOfWork _uow;
    private readonly ILogger<TenantService> _logger;

    public TenantService(IUnitOfWork uow, ILogger<TenantService> logger)
    {
        _uow = uow;
        _logger = logger;
    }

    public async Task<IEnumerable<Tenant>> GetAllAsync() =>
        await _uow.Tenants.Query().OrderBy(t => t.FullName).ToListAsync();

    public async Task<Tenant?> GetByIdAsync(int id) => await _uow.Tenants.GetByIdAsync(id);

    public async Task<IEnumerable<Tenant>> SearchAsync(string keyword)
    {
        keyword = keyword.ToLower();
        return await _uow.Tenants.Query()
            .Where(t => t.FullName.ToLower().Contains(keyword) ||
                        t.Phone.Contains(keyword) ||
                        (t.CCCD != null && t.CCCD.Contains(keyword)) ||
                        (t.Email != null && t.Email.ToLower().Contains(keyword)))
            .ToListAsync();
    }

    public async Task<bool> CreateAsync(Tenant tenant)
    {
        try
        {
            tenant.CreatedAt = DateTime.Now;
            await _uow.Tenants.AddAsync(tenant);
            await _uow.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating tenant {Name}", tenant.FullName);
            return false;
        }
    }

    public async Task<bool> UpdateAsync(Tenant tenant)
    {
        try
        {
            _uow.Tenants.Update(tenant);
            await _uow.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating tenant {Id}", tenant.Id);
            return false;
        }
    }

    public async Task<bool> DeleteAsync(int id)
    {
        try
        {
            var tenant = await _uow.Tenants.GetByIdAsync(id);
            if (tenant == null) return false;
            _uow.Tenants.Remove(tenant);
            await _uow.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting tenant {Id}", id);
            return false;
        }
    }
}
