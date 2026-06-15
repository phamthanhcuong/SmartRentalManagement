using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RentalManagementSystem.Application.Interfaces;
using RentalManagementSystem.Domain.Entities;

namespace RentalManagementSystem.Application.Services;

public class AuditService : IAuditService
{
    private readonly IUnitOfWork _uow;
    private readonly ICurrentUser _currentUser;
    private readonly ILogger<AuditService> _logger;

    public AuditService(IUnitOfWork uow, ICurrentUser currentUser, ILogger<AuditService> logger)
    {
        _uow = uow;
        _currentUser = currentUser;
        _logger = logger;
    }

    public async Task LogAsync(string action, string entityType, string? entityRef = null, string? details = null)
    {
        try
        {
            await _uow.AuditLogs.AddAsync(new AuditLog
            {
                UserName = string.IsNullOrWhiteSpace(_currentUser.UserName) ? "(hệ thống)" : _currentUser.UserName,
                Action = action,
                EntityType = entityType,
                EntityRef = entityRef,
                Details = details,
                Timestamp = DateTime.Now,
                CreatedAt = DateTime.Now
            });
            await _uow.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            // Nhật ký không được làm gãy nghiệp vụ chính
            _logger.LogError(ex, "Error writing audit log: {Action}", action);
        }
    }

    public async Task<IEnumerable<AuditLog>> GetRecentAsync(int take = 500) =>
        await _uow.AuditLogs.Query()
            .OrderByDescending(a => a.Timestamp)
            .Take(take)
            .ToListAsync();
}
