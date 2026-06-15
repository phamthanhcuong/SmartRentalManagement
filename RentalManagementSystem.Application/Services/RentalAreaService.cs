using Microsoft.Extensions.Logging;
using RentalManagementSystem.Application.Interfaces;
using RentalManagementSystem.Domain.Entities;

namespace RentalManagementSystem.Application.Services;

public class RentalAreaService : IRentalAreaService
{
    private readonly IUnitOfWork _uow;
    private readonly ILogger<RentalAreaService> _logger;

    public RentalAreaService(IUnitOfWork uow, ILogger<RentalAreaService> logger)
    {
        _uow = uow;
        _logger = logger;
    }

    public async Task<IEnumerable<RentalArea>> GetAllAsync() => await _uow.RentalAreas.GetAllAsync();

    public async Task<RentalArea?> GetByIdAsync(int id) => await _uow.RentalAreas.GetByIdAsync(id);

    public async Task<bool> CreateAsync(RentalArea area)
    {
        try
        {
            area.CreatedAt = DateTime.Now;
            await _uow.RentalAreas.AddAsync(area);
            await _uow.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating rental area");
            return false;
        }
    }

    public async Task<bool> UpdateAsync(RentalArea area)
    {
        try
        {
            _uow.RentalAreas.Update(area);
            await _uow.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating rental area {Id}", area.Id);
            return false;
        }
    }

    public async Task<bool> DeleteAsync(int id)
    {
        try
        {
            var area = await _uow.RentalAreas.GetByIdAsync(id);
            if (area == null) return false;
            _uow.RentalAreas.Remove(area);
            await _uow.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting rental area {Id}", id);
            return false;
        }
    }
}
