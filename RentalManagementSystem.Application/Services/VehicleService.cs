using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RentalManagementSystem.Application.Interfaces;
using RentalManagementSystem.Domain.Entities;

namespace RentalManagementSystem.Application.Services;

public class VehicleService : IVehicleService
{
    private readonly IUnitOfWork _uow;
    private readonly ILogger<VehicleService> _logger;

    public VehicleService(IUnitOfWork uow, ILogger<VehicleService> logger)
    {
        _uow = uow;
        _logger = logger;
    }

    public async Task<IEnumerable<Vehicle>> GetByTenantAsync(int tenantId) =>
        await _uow.Vehicles.Query()
            .Where(v => v.TenantId == tenantId)
            .OrderByDescending(v => v.IsActive).ThenBy(v => v.LicensePlate)
            .ToListAsync();

    public async Task<IEnumerable<Vehicle>> GetAllActiveAsync() =>
        await _uow.Vehicles.Query()
            .Include(v => v.Tenant)
            .Where(v => v.IsActive)
            .OrderBy(v => v.LicensePlate)
            .ToListAsync();

    public async Task<bool> AddAsync(Vehicle vehicle)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(vehicle.LicensePlate)) return false;
            vehicle.LicensePlate = vehicle.LicensePlate.Trim().ToUpper();

            // Không cho trùng biển số đang hoạt động
            var dup = await _uow.Vehicles.FirstOrDefaultAsync(v => v.LicensePlate == vehicle.LicensePlate && v.IsActive);
            if (dup != null) return false;

            vehicle.CreatedAt = DateTime.Now;
            await _uow.Vehicles.AddAsync(vehicle);
            await _uow.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding vehicle {Plate}", vehicle.LicensePlate);
            return false;
        }
    }

    public async Task<bool> UpdateAsync(Vehicle vehicle)
    {
        try
        {
            _uow.Vehicles.Update(vehicle);
            await _uow.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating vehicle {Id}", vehicle.Id);
            return false;
        }
    }

    public async Task<IEnumerable<VehicleLookupItem>> SearchAsync(string? keyword)
    {
        var vehicles = await _uow.Vehicles.Query()
            .Include(v => v.Tenant)
            .Where(v => v.IsActive)
            .ToListAsync();

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var k = keyword.Trim().ToLower();
            vehicles = vehicles.Where(v =>
                v.LicensePlate.ToLower().Contains(k)
                || (v.Brand ?? "").ToLower().Contains(k)
                || v.Tenant.FullName.ToLower().Contains(k)
                || v.Tenant.Phone.Contains(k)).ToList();
        }

        // Lấy phòng/khu đang ở qua hợp đồng hiệu lực của chủ xe
        var tenantIds = vehicles.Select(v => v.TenantId).Distinct().ToList();
        var contracts = await _uow.Contracts.Query()
            .Include(c => c.Room).ThenInclude(r => r.RentalArea)
            .Where(c => c.Status == Domain.Enums.ContractStatus.Active && tenantIds.Contains(c.TenantId))
            .ToListAsync();

        return vehicles
            .OrderBy(v => v.LicensePlate)
            .Select(v =>
            {
                var c = contracts.FirstOrDefault(x => x.TenantId == v.TenantId);
                return new VehicleLookupItem(
                    v.LicensePlate, v.VehicleType, v.Brand, v.Color,
                    v.Tenant.FullName, v.Tenant.Phone,
                    c?.Room.RoomCode ?? "(chưa thuê)",
                    c?.Room.RentalArea?.AreaName ?? "",
                    v.RegisterDate);
            })
            .ToList();
    }

    public async Task<bool> RemoveAsync(int id)
    {
        try
        {
            var v = await _uow.Vehicles.GetByIdAsync(id);
            if (v == null) return false;
            _uow.Vehicles.Remove(v);
            await _uow.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing vehicle {Id}", id);
            return false;
        }
    }
}
