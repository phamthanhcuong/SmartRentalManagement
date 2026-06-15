using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RentalManagementSystem.Application.Interfaces;
using RentalManagementSystem.Domain.Entities;

namespace RentalManagementSystem.Application.Services;

public class ContractSubscriptionService : IContractSubscriptionService
{
    private readonly IUnitOfWork _uow;
    private readonly ILogger<ContractSubscriptionService> _logger;

    public ContractSubscriptionService(IUnitOfWork uow, ILogger<ContractSubscriptionService> logger)
    {
        _uow = uow;
        _logger = logger;
    }

    public async Task<IEnumerable<ContractSubscription>> GetByContractAsync(int contractId) =>
        await _uow.ContractSubscriptions.Query()
            .Include(s => s.Service)
            .Where(s => s.ContractId == contractId)
            .ToListAsync();

    public async Task<bool> AddAsync(ContractSubscription subscription)
    {
        try
        {
            // Không cho đăng ký trùng dịch vụ trên cùng hợp đồng
            var existed = await _uow.ContractSubscriptions
                .FirstOrDefaultAsync(s => s.ContractId == subscription.ContractId && s.ServiceId == subscription.ServiceId);
            if (existed != null) return false;

            subscription.CreatedAt = DateTime.Now;
            await _uow.ContractSubscriptions.AddAsync(subscription);
            await _uow.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding subscription for contract {ContractId}", subscription.ContractId);
            return false;
        }
    }

    public async Task<bool> RemoveAsync(int id)
    {
        try
        {
            var sub = await _uow.ContractSubscriptions.GetByIdAsync(id);
            if (sub == null) return false;
            _uow.ContractSubscriptions.Remove(sub);
            await _uow.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing subscription {Id}", id);
            return false;
        }
    }
}
