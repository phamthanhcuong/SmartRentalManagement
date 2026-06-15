using Microsoft.Extensions.Logging;
using RentalManagementSystem.Application.Interfaces;
using RentalManagementSystem.Domain.Entities;

namespace RentalManagementSystem.Application.Services;

public class SystemSettingService : ISystemSettingService
{
    private readonly IUnitOfWork _uow;
    private readonly ILogger<SystemSettingService> _logger;

    public SystemSettingService(IUnitOfWork uow, ILogger<SystemSettingService> logger)
    {
        _uow = uow;
        _logger = logger;
    }

    public async Task<SystemSetting> GetAsync()
    {
        var s = await _uow.SystemSettings.GetByIdAsync(1);
        if (s == null)
        {
            // Tự tạo nếu chưa có (đề phòng DB cũ)
            s = new SystemSetting { Id = 1, CreatedAt = DateTime.Now };
            await _uow.SystemSettings.AddAsync(s);
            await _uow.SaveChangesAsync();
        }
        return s;
    }

    public async Task<bool> UpdateAsync(SystemSetting setting)
    {
        try
        {
            setting.UpdatedAt = DateTime.Now;
            _uow.SystemSettings.Update(setting);
            await _uow.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating system settings");
            return false;
        }
    }
}
