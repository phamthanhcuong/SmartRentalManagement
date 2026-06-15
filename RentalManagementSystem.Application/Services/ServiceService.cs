using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RentalManagementSystem.Application.Interfaces;

namespace RentalManagementSystem.Application.Services;

public class ServiceService : IServiceService
{
    private readonly IUnitOfWork _uow;
    private readonly ILogger<ServiceService> _logger;

    public ServiceService(IUnitOfWork uow, ILogger<ServiceService> logger)
    {
        _uow = uow;
        _logger = logger;
    }

    public async Task<IEnumerable<Domain.Entities.Service>> GetAllAsync() =>
        await _uow.Services.Query().Where(s => s.IsActive).OrderBy(s => s.ServiceName).ToListAsync();

    public async Task<Domain.Entities.Service?> GetByIdAsync(int id) => await _uow.Services.GetByIdAsync(id);

    public async Task<bool> CreateAsync(Domain.Entities.Service service)
    {
        try
        {
            service.CreatedAt = DateTime.Now;
            await _uow.Services.AddAsync(service);
            await _uow.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating service");
            return false;
        }
    }

    public async Task<bool> UpdateAsync(Domain.Entities.Service service)
    {
        try
        {
            _uow.Services.Update(service);
            await _uow.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating service {Id}", service.Id);
            return false;
        }
    }

    public async Task<bool> DeleteAsync(int id)
    {
        try
        {
            var service = await _uow.Services.GetByIdAsync(id);
            if (service == null) return false;
            _uow.Services.Remove(service);
            await _uow.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting service {Id}", id);
            return false;
        }
    }
}
