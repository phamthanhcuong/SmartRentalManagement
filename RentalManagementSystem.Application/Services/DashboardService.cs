using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RentalManagementSystem.Application.Interfaces;
using RentalManagementSystem.Domain.Enums;

namespace RentalManagementSystem.Application.Services;

public class DashboardService : IDashboardService
{
    private readonly IUnitOfWork _uow;
    private readonly ILogger<DashboardService> _logger;

    public DashboardService(IUnitOfWork uow, ILogger<DashboardService> logger)
    {
        _uow = uow;
        _logger = logger;
    }

    public async Task<DashboardStats> GetStatsAsync()
    {
        var now = DateTime.Now;

        // Tự động bảo trì trạng thái theo thời gian mỗi khi mở Dashboard
        await UpdateExpiredContractsAsync(now);
        await MarkOverdueInvoicesAsync(now);

        var totalRooms = await _uow.Rooms.CountAsync();
        var availableRooms = await _uow.Rooms.CountAsync(r => r.Status == RoomStatus.Available);
        var occupiedRooms = await _uow.Rooms.CountAsync(r => r.Status == RoomStatus.Occupied);
        var totalTenants = await _uow.Tenants.CountAsync();
        var totalUnpaid = (await _uow.Invoices.Query()
            .Where(i => i.Status == InvoiceStatus.Unpaid || i.Status == InvoiceStatus.Overdue || i.Status == InvoiceStatus.PartiallyPaid)
            .Select(i => i.TotalAmount - i.PaidAmount)
            .ToListAsync()).Sum();
        var monthlyRevenue = (await _uow.Invoices.Query()
            .Where(i => i.Month == now.Month && i.Year == now.Year)
            .Select(i => i.PaidAmount)
            .ToListAsync()).Sum();
        var expiringContracts = await _uow.Contracts.CountAsync(c =>
            c.Status == ContractStatus.Active && c.EndDate <= now.AddDays(30));

        return new DashboardStats(totalRooms, availableRooms, occupiedRooms, totalTenants, totalUnpaid, monthlyRevenue, expiringContracts);
    }

    private async Task UpdateExpiredContractsAsync(DateTime now)
    {
        var expired = await _uow.Contracts.Query()
            .Where(c => c.Status == ContractStatus.Active && c.EndDate < now.Date)
            .ToListAsync();
        if (expired.Count == 0) return;
        foreach (var c in expired) { c.Status = ContractStatus.Expired; _uow.Contracts.Update(c); }
        await _uow.SaveChangesAsync();
    }

    private async Task MarkOverdueInvoicesAsync(DateTime now)
    {
        var overdue = await _uow.Invoices.Query()
            .Where(i => (i.Status == InvoiceStatus.Unpaid || i.Status == InvoiceStatus.PartiallyPaid)
                     && i.DueDate != null && i.DueDate < now.Date && i.PaidAmount < i.TotalAmount)
            .ToListAsync();
        if (overdue.Count == 0) return;
        foreach (var i in overdue) { i.Status = InvoiceStatus.Overdue; _uow.Invoices.Update(i); }
        await _uow.SaveChangesAsync();
    }

    public async Task<IEnumerable<MonthlyRevenue>> GetMonthlyRevenueAsync(int year)
    {
        var result = new List<MonthlyRevenue>();
        for (int m = 1; m <= 12; m++)
        {
            var revenue = (await _uow.Invoices.Query()
                .Where(i => i.Month == m && i.Year == year)
                .Select(i => i.PaidAmount)
                .ToListAsync()).Sum();
            var expense = (await _uow.IncomeExpenses.Query()
                .Where(ie => ie.TransactionDate.Month == m && ie.TransactionDate.Year == year
                          && ie.Type == TransactionType.Expense && !ie.IsDeposit)
                .Select(ie => ie.Amount)
                .ToListAsync()).Sum();
            result.Add(new MonthlyRevenue(m, revenue, expense));
        }
        return result;
    }

    public async Task<DashboardAlerts> GetAlertsAsync()
    {
        var now = DateTime.Now;
        int month = now.Month, year = now.Year;

        // Phòng đang thuê (có HĐ hiệu lực)
        var activeRoomIds = await _uow.Contracts.Query()
            .Where(c => c.Status == ContractStatus.Active)
            .Select(c => c.RoomId).Distinct().ToListAsync();

        var roomsWithReading = await _uow.UtilityReadings.Query()
            .Where(u => u.Month == month && u.Year == year)
            .Select(u => u.RoomId).Distinct().ToListAsync();

        var roomsWithInvoice = await _uow.Invoices.Query()
            .Where(i => i.Month == month && i.Year == year)
            .Select(i => i.RoomId).Distinct().ToListAsync();

        var roomsMissingReading = activeRoomIds.Except(roomsWithReading).Count();
        var roomsNoInvoice = activeRoomIds.Except(roomsWithInvoice).Count();

        var overdueQuery = _uow.Invoices.Query().Where(i => i.Status == InvoiceStatus.Overdue);
        var overdueCount = await overdueQuery.CountAsync();
        var overdueAmount = (await overdueQuery.Select(i => i.TotalAmount - i.PaidAmount).ToListAsync()).Sum();

        var expiring = await _uow.Contracts.CountAsync(c =>
            c.Status == ContractStatus.Active && c.EndDate <= now.AddDays(30) && c.EndDate >= now.Date);

        return new DashboardAlerts(roomsMissingReading, overdueCount, overdueAmount, expiring, roomsNoInvoice);
    }

    public async Task<IEnumerable<RoomOccupancyRate>> GetOccupancyRateAsync()
    {
        var areas = await _uow.RentalAreas.Query().Include(a => a.Rooms).ToListAsync();
        return areas.Select(a => new RoomOccupancyRate(
            a.AreaName,
            a.Rooms.Count,
            a.Rooms.Count(r => r.Status == RoomStatus.Occupied)
        ));
    }
}
