using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RentalManagementSystem.Application.Interfaces;
using RentalManagementSystem.Domain.Entities;
using RentalManagementSystem.Domain.Enums;

namespace RentalManagementSystem.Application.Services;

public class ContractService : IContractService
{
    private readonly IUnitOfWork _uow;
    private readonly IAuditService _audit;
    private readonly ILogger<ContractService> _logger;

    public ContractService(IUnitOfWork uow, IAuditService audit, ILogger<ContractService> logger)
    {
        _uow = uow;
        _audit = audit;
        _logger = logger;
    }

    public async Task<IEnumerable<Contract>> GetAllAsync() =>
        await _uow.Contracts.Query()
            .Include(c => c.Tenant)
            .Include(c => c.Room).ThenInclude(r => r.RentalArea)
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync();

    public async Task<Contract?> GetByIdAsync(int id) =>
        await _uow.Contracts.Query()
            .Include(c => c.Tenant)
            .Include(c => c.Room).ThenInclude(r => r.RentalArea)
            .FirstOrDefaultAsync(c => c.Id == id);

    public async Task<Contract?> GetActiveByRoomAsync(int roomId) =>
        await _uow.Contracts.Query()
            .Include(c => c.Tenant)
            .FirstOrDefaultAsync(c => c.RoomId == roomId && c.Status == ContractStatus.Active);

    public async Task<bool> CreateAsync(Contract contract)
    {
        try
        {
            // Ràng buộc: phòng đang có hợp đồng hiệu lực thì không cho ký mới (chống double-booking)
            var activeExists = await _uow.Contracts.FirstOrDefaultAsync(
                c => c.RoomId == contract.RoomId && c.Status == ContractStatus.Active);
            if (activeExists != null)
            {
                _logger.LogWarning("Phòng {RoomId} đã có hợp đồng hiệu lực {No}", contract.RoomId, activeExists.ContractNo);
                return false;
            }

            contract.CreatedAt = DateTime.Now;
            if (string.IsNullOrEmpty(contract.ContractNo))
                contract.ContractNo = await GenerateContractNoAsync();
            await _uow.Contracts.AddAsync(contract);

            // Update room status
            var room = await _uow.Rooms.GetByIdAsync(contract.RoomId);
            if (room != null)
            {
                room.Status = RoomStatus.Occupied;
                _uow.Rooms.Update(room);
            }

            // Hạch toán tiền cọc nhận vào sổ quỹ
            if (contract.DepositAmount > 0)
            {
                await _uow.IncomeExpenses.AddAsync(new IncomeExpense
                {
                    Type = TransactionType.Income,
                    Category = "Tiền cọc", IsDeposit = true,
                    Amount = contract.DepositAmount,
                    Description = $"Nhận cọc hợp đồng {contract.ContractNo}",
                    TransactionDate = DateTime.Now,
                    RoomId = contract.RoomId,
                    Reference = contract.ContractNo
                });
            }

            await _uow.SaveChangesAsync();
            await _audit.LogAsync("Tạo hợp đồng", "Hợp đồng", contract.ContractNo, $"Cọc {contract.DepositAmount:N0}đ, thuê {contract.MonthlyRent:N0}đ/tháng");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating contract {No}", contract.ContractNo);
            return false;
        }
    }

    public async Task<bool> UpdateAsync(Contract contract)
    {
        try
        {
            _uow.Contracts.Update(contract);
            await _uow.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating contract {Id}", contract.Id);
            return false;
        }
    }

    public async Task<bool> TerminateAsync(int id, decimal depositDeduction = 0)
    {
        try
        {
            var contract = await _uow.Contracts.GetByIdAsync(id);
            if (contract == null) return false;
            contract.Status = ContractStatus.Terminated;
            contract.CheckoutDate = DateTime.Now;
            _uow.Contracts.Update(contract);

            var room = await _uow.Rooms.GetByIdAsync(contract.RoomId);
            if (room != null)
            {
                room.Status = RoomStatus.Available;
                _uow.Rooms.Update(room);
            }

            // Hoàn cọc (sau khi trừ khấu trừ) -> ghi chi vào sổ quỹ
            var refund = contract.DepositAmount - depositDeduction;
            if (refund > 0)
            {
                await _uow.IncomeExpenses.AddAsync(new IncomeExpense
                {
                    Type = TransactionType.Expense,
                    Category = "Hoàn cọc", IsDeposit = true,
                    Amount = refund,
                    Description = $"Hoàn cọc thanh lý hợp đồng {contract.ContractNo}" +
                                  (depositDeduction > 0 ? $" (đã trừ {depositDeduction:N0}đ)" : ""),
                    TransactionDate = DateTime.Now,
                    RoomId = contract.RoomId,
                    Reference = contract.ContractNo
                });
            }

            await _uow.SaveChangesAsync();
            await _audit.LogAsync("Trả phòng / Thanh lý", "Hợp đồng", contract.ContractNo,
                $"Hoàn cọc {refund:N0}đ" + (depositDeduction > 0 ? $", trừ {depositDeduction:N0}đ" : ""));
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error terminating contract {Id}", id);
            return false;
        }
    }

    public async Task<decimal> GetOutstandingByContractAsync(int contractId)
    {
        var contract = await _uow.Contracts.GetByIdAsync(contractId);
        if (contract == null) return 0;
        return (await _uow.Invoices.Query()
            .Where(i => i.RoomId == contract.RoomId
                     && i.Status != InvoiceStatus.Paid
                     && i.Status != InvoiceStatus.Cancelled)
            .Select(i => i.TotalAmount - i.PaidAmount)
            .ToListAsync()).Sum();
    }

    public async Task<bool> CheckoutAsync(int id, decimal depositDeduction = 0)
        => await TerminateAsync(id, depositDeduction);

    public async Task<bool> TransferAsync(int oldContractId, int newTenantId, decimal newDeposit, bool keepDeposit)
    {
        try
        {
            var old = await _uow.Contracts.Query()
                .Include(c => c.Subscriptions)
                .FirstOrDefaultAsync(c => c.Id == oldContractId);
            if (old == null || old.Status != ContractStatus.Active) return false;
            if (newTenantId == old.TenantId) return false; // phải là khách khác

            var today = DateTime.Now;

            // 1) Kết thúc HĐ cũ (phòng vẫn có người nên KHÔNG đổi sang trống)
            old.Status = ContractStatus.Terminated;
            old.CheckoutDate = today;
            old.Note = $"{old.Note} | Nhường phòng cho khách #{newTenantId} ngày {today:dd/MM/yyyy}".Trim(' ', '|');
            _uow.Contracts.Update(old);

            // 2) Tạo HĐ mới cho khách nhận, kế thừa điều khoản phòng
            var newContract = new Contract
            {
                ContractNo = await GenerateContractNoAsync(),
                TenantId = newTenantId,
                RoomId = old.RoomId,
                StartDate = today,
                EndDate = old.EndDate > today ? old.EndDate : today.AddMonths(6),
                MonthlyRent = old.MonthlyRent,
                PaymentDueDay = old.PaymentDueDay,
                OccupantCount = old.OccupantCount,
                DepositAmount = keepDeposit ? old.DepositAmount : newDeposit,
                Status = ContractStatus.Active,
                CreatedAt = today,
                Note = $"Nhận nhường phòng từ HĐ {old.ContractNo}"
            };
            await _uow.Contracts.AddAsync(newContract);
            await _uow.SaveChangesAsync(); // để có Id cho subscription

            // 3) Sao chép dịch vụ đăng ký sang HĐ mới
            foreach (var sub in old.Subscriptions)
            {
                await _uow.ContractSubscriptions.AddAsync(new ContractSubscription
                {
                    ContractId = newContract.Id,
                    ServiceId = sub.ServiceId,
                    Quantity = sub.Quantity,
                    UnitPrice = sub.UnitPrice,
                    IsPerPerson = sub.IsPerPerson,
                    CreatedAt = today
                });
            }

            // 4) Xử lý cọc
            if (!keepDeposit)
            {
                if (old.DepositAmount > 0)
                    await _uow.IncomeExpenses.AddAsync(new IncomeExpense
                    {
                        Type = TransactionType.Expense, Category = "Hoàn cọc", IsDeposit = true,
                        Amount = old.DepositAmount,
                        Description = $"Hoàn cọc khi nhường phòng - HĐ {old.ContractNo}",
                        TransactionDate = today, RoomId = old.RoomId, Reference = old.ContractNo
                    });
                if (newDeposit > 0)
                    await _uow.IncomeExpenses.AddAsync(new IncomeExpense
                    {
                        Type = TransactionType.Income, Category = "Tiền cọc", IsDeposit = true,
                        Amount = newDeposit,
                        Description = $"Nhận cọc HĐ mới {newContract.ContractNo}",
                        TransactionDate = today, RoomId = newContract.RoomId, Reference = newContract.ContractNo
                    });
            }

            await _uow.SaveChangesAsync();
            await _audit.LogAsync("Nhường phòng", "Hợp đồng", old.ContractNo, $"Tạo HĐ mới {newContract.ContractNo}");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error transferring contract {Id}", oldContractId);
            return false;
        }
    }

    public async Task<int> UpdateExpiredStatusesAsync()
    {
        try
        {
            var today = DateTime.Today;
            var expired = await _uow.Contracts.Query()
                .Where(c => c.Status == ContractStatus.Active && c.EndDate < today)
                .ToListAsync();
            foreach (var c in expired)
            {
                c.Status = ContractStatus.Expired;
                _uow.Contracts.Update(c);
            }
            if (expired.Count > 0) await _uow.SaveChangesAsync();
            return expired.Count;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating expired contracts");
            return 0;
        }
    }

    public async Task<bool> RenewAsync(int id, DateTime newEndDate)
    {
        try
        {
            var contract = await _uow.Contracts.GetByIdAsync(id);
            if (contract == null) return false;
            contract.EndDate = newEndDate;
            contract.Status = ContractStatus.Active;
            _uow.Contracts.Update(contract);
            await _uow.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error renewing contract {Id}", id);
            return false;
        }
    }

    public async Task<string> GenerateContractNoAsync()
    {
        // Số chạy dựa trên SỐ LỚN NHẤT cùng tiền tố (kể cả bản ghi đã xóa) -> không trùng sau khi xóa
        var prefix = $"HĐ{DateTime.Now:yyyyMM}";
        var existing = await _uow.Contracts.Query()
            .IgnoreQueryFilters()
            .Where(c => c.ContractNo.StartsWith(prefix))
            .Select(c => c.ContractNo)
            .ToListAsync();
        int max = 0;
        foreach (var no in existing)
        {
            var tail = no.Length > prefix.Length ? no.Substring(prefix.Length) : "";
            if (int.TryParse(tail, out var v) && v > max) max = v;
        }
        return $"{prefix}{(max + 1):D4}";
    }
}
