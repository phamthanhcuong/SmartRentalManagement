using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RentalManagementSystem.Application.Interfaces;
using RentalManagementSystem.Domain.Entities;
using RentalManagementSystem.Domain.Enums;

namespace RentalManagementSystem.Application.Services;

public class IncomeExpenseService : IIncomeExpenseService
{
    private readonly IUnitOfWork _uow;
    private readonly ILogger<IncomeExpenseService> _logger;

    public IncomeExpenseService(IUnitOfWork uow, ILogger<IncomeExpenseService> logger)
    {
        _uow = uow;
        _logger = logger;
    }

    public async Task<IEnumerable<IncomeExpense>> GetAllAsync() =>
        await _uow.IncomeExpenses.Query()
            .Include(ie => ie.Room)
            .OrderByDescending(ie => ie.TransactionDate)
            .ToListAsync();

    public async Task<IEnumerable<IncomeExpense>> GetByTypeAsync(TransactionType type) =>
        await _uow.IncomeExpenses.Query()
            .Include(ie => ie.Room)
            .Where(ie => ie.Type == type)
            .OrderByDescending(ie => ie.TransactionDate)
            .ToListAsync();

    public async Task<IEnumerable<IncomeExpense>> GetByMonthYearAsync(int month, int year) =>
        await _uow.IncomeExpenses.Query()
            .Include(ie => ie.Room)
            .Where(ie => ie.TransactionDate.Month == month && ie.TransactionDate.Year == year)
            .OrderByDescending(ie => ie.TransactionDate)
            .ToListAsync();

    public async Task<bool> CreateAsync(IncomeExpense item)
    {
        try
        {
            item.CreatedAt = DateTime.Now;
            await _uow.IncomeExpenses.AddAsync(item);
            await _uow.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating income/expense");
            return false;
        }
    }

    public async Task<bool> UpdateAsync(IncomeExpense item)
    {
        try
        {
            _uow.IncomeExpenses.Update(item);
            await _uow.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating income/expense {Id}", item.Id);
            return false;
        }
    }

    public async Task<bool> DeleteAsync(int id)
    {
        try
        {
            var item = await _uow.IncomeExpenses.GetByIdAsync(id);
            if (item == null) return false;
            _uow.IncomeExpenses.Remove(item);
            await _uow.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting income/expense {Id}", id);
            return false;
        }
    }

    public async Task<decimal> GetTotalIncomeAsync(int month, int year) =>
        (await _uow.IncomeExpenses.Query()
            .Where(ie => ie.Type == TransactionType.Income && ie.TransactionDate.Month == month && ie.TransactionDate.Year == year)
            .Select(ie => ie.Amount)
            .ToListAsync()).Sum();

    public async Task<decimal> GetTotalExpenseAsync(int month, int year) =>
        (await _uow.IncomeExpenses.Query()
            .Where(ie => ie.Type == TransactionType.Expense && ie.TransactionDate.Month == month && ie.TransactionDate.Year == year)
            .Select(ie => ie.Amount)
            .ToListAsync()).Sum();
}
