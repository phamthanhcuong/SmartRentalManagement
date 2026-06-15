using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RentalManagementSystem.Application.Interfaces;
using RentalManagementSystem.Domain.Entities;
using RentalManagementSystem.Domain.Enums;

namespace RentalManagementSystem.Application.Services;

public class InvoiceService : IInvoiceService
{
    private readonly IUnitOfWork _uow;
    private readonly IAuditService _audit;
    private readonly ISystemSettingService _settings;
    private readonly ILogger<InvoiceService> _logger;

    public InvoiceService(IUnitOfWork uow, IAuditService audit, ISystemSettingService settings, ILogger<InvoiceService> logger)
    {
        _uow = uow;
        _audit = audit;
        _settings = settings;
        _logger = logger;
    }

    public async Task<IEnumerable<Invoice>> GetAllAsync() =>
        await _uow.Invoices.Query()
            .Include(i => i.Room).ThenInclude(r => r.RentalArea)
            .Include(i => i.Tenant)
            .OrderByDescending(i => i.InvoiceDate)
            .ToListAsync();

    public async Task<Invoice?> GetByIdAsync(int id) =>
        await _uow.Invoices.Query()
            .Include(i => i.Room)
            .Include(i => i.Tenant)
            .Include(i => i.InvoiceDetails).ThenInclude(d => d.Service)
            .FirstOrDefaultAsync(i => i.Id == id);

    public async Task<IEnumerable<Invoice>> GetByRoomAsync(int roomId) =>
        await _uow.Invoices.Query()
            .Include(i => i.Tenant)
            .Where(i => i.RoomId == roomId)
            .OrderByDescending(i => i.InvoiceDate)
            .ToListAsync();

    public async Task<IEnumerable<Invoice>> GetByMonthYearAsync(int month, int year) =>
        await _uow.Invoices.Query()
            .Include(i => i.Room)
            .Include(i => i.Tenant)
            .Where(i => i.Month == month && i.Year == year)
            .ToListAsync();

    public async Task<bool> CreateAsync(Invoice invoice)
    {
        try
        {
            if (string.IsNullOrEmpty(invoice.InvoiceNo))
                invoice.InvoiceNo = await GenerateInvoiceNoAsync();
            invoice.CreatedAt = DateTime.Now;
            invoice.TotalAmount = Math.Round(invoice.RentAmount + invoice.ElectricAmount + invoice.WaterAmount + invoice.ServiceAmount - invoice.DiscountAmount + invoice.PreviousDebt, 0, MidpointRounding.AwayFromZero);
            await _uow.Invoices.AddAsync(invoice);
            await _uow.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating invoice {No}", invoice.InvoiceNo);
            return false;
        }
    }

    public async Task<bool> UpdateAsync(Invoice invoice)
    {
        try
        {
            invoice.TotalAmount = Math.Round(invoice.RentAmount + invoice.ElectricAmount + invoice.WaterAmount + invoice.ServiceAmount - invoice.DiscountAmount + invoice.PreviousDebt, 0, MidpointRounding.AwayFromZero);
            _uow.Invoices.Update(invoice);
            await _uow.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating invoice {Id}", invoice.Id);
            return false;
        }
    }

    public async Task<bool> PayAsync(int id, decimal amount)
    {
        try
        {
            if (amount <= 0) return false;
            var invoice = await _uow.Invoices.GetByIdAsync(id);
            if (invoice == null || invoice.Status == InvoiceStatus.Cancelled) return false;

            // Không cho thu vượt số còn nợ
            var outstanding = invoice.TotalAmount - invoice.PaidAmount;
            if (outstanding <= 0) return false;
            var applied = Math.Min(amount, outstanding);

            invoice.PaidAmount += applied;

            // Xác định trạng thái chính xác theo số tiền đã thu
            if (invoice.PaidAmount >= invoice.TotalAmount)
            {
                invoice.Status = InvoiceStatus.Paid;
                invoice.PaidDate = DateTime.Now;
            }
            else
            {
                invoice.Status = InvoiceStatus.PartiallyPaid; // trả một phần
                invoice.PaidDate = null;                       // chưa tất toán
            }
            _uow.Invoices.Update(invoice);

            // Ghi nhận khoản thu thực tế vào sổ quỹ
            var income = new IncomeExpense
            {
                Type = TransactionType.Income,
                Category = "Thu tiền hóa đơn",
                Amount = applied,
                Description = $"Thu tiền hóa đơn {invoice.InvoiceNo}",
                TransactionDate = DateTime.Now,
                RoomId = invoice.RoomId,
                InvoiceId = invoice.Id,
                Reference = invoice.InvoiceNo
            };
            await _uow.IncomeExpenses.AddAsync(income);
            await _uow.SaveChangesAsync();
            await _audit.LogAsync("Thu tiền", "Hóa đơn", invoice.InvoiceNo, $"Thu {applied:N0}đ, trạng thái: {invoice.Status}");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error paying invoice {Id}", id);
            return false;
        }
    }

    public async Task<bool> DeleteAsync(int id)
    {
        try
        {
            var invoice = await _uow.Invoices.GetByIdAsync(id);
            if (invoice == null) return false;

            // Đồng bộ sổ quỹ: xóa các bút toán Thu đã ghi cho hóa đơn này
            var linkedIncomes = await _uow.IncomeExpenses.FindAsync(ie => ie.InvoiceId == id);
            foreach (var inc in linkedIncomes)
                _uow.IncomeExpenses.Remove(inc);

            _uow.Invoices.Remove(invoice);
            await _uow.SaveChangesAsync();
            await _audit.LogAsync("Xóa hóa đơn", "Hóa đơn", invoice.InvoiceNo, $"Tổng {invoice.TotalAmount:N0}đ");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting invoice {Id}", id);
            return false;
        }
    }

    public async Task<string> GenerateInvoiceNoAsync()
    {
        var prefix = $"HD{DateTime.Now:yyyyMM}";
        var seq = await NextSequenceAsync(prefix);
        return $"{prefix}{seq:D4}";
    }

    /// <summary>
    /// Số tiếp theo dựa trên SỐ LỚN NHẤT đang có cùng tiền tố (kể cả bản ghi đã xóa mềm),
    /// tránh trùng số sau khi xóa — thay cho cách đếm Count() cũ bị lỗi.
    /// </summary>
    private async Task<int> NextSequenceAsync(string prefix)
    {
        var existing = await _uow.Invoices.Query()
            .IgnoreQueryFilters()
            .Where(i => i.InvoiceNo.StartsWith(prefix))
            .Select(i => i.InvoiceNo)
            .ToListAsync();
        int max = 0;
        foreach (var no in existing)
        {
            var tail = no.Length > prefix.Length ? no.Substring(prefix.Length) : "";
            if (int.TryParse(tail, out var v) && v > max) max = v;
        }
        return max + 1;
    }

    public async Task GenerateMonthlyInvoicesAsync(int month, int year)
    {
        var activeContracts = await _uow.Contracts.Query()
            .Include(c => c.Room)
            .Include(c => c.Tenant)
            .Include(c => c.Subscriptions).ThenInclude(s => s.Service)
            .Where(c => c.Status == ContractStatus.Active)
            .ToListAsync();

        // Cấu hình (phí trễ hạn)
        var setting = await _settings.GetAsync();

        // Số ngày tối đa của tháng để chốt hạn thanh toán hợp lệ
        var daysInMonth = DateTime.DaysInMonth(year, month);
        // Tiền tố theo đúng kỳ hóa đơn; số chạy dựa trên max thật để không trùng
        var noPrefix = $"HD{year:D4}{month:D2}";
        var seq = await NextSequenceAsync(noPrefix) - 1;
        var startSeq = seq;

        foreach (var contract in activeContracts)
        {
            var existing = await _uow.Invoices.FirstOrDefaultAsync(i => i.RoomId == contract.RoomId && i.Month == month && i.Year == year);
            if (existing != null) continue;

            var utility = await _uow.UtilityReadings.FirstOrDefaultAsync(u => u.RoomId == contract.RoomId && u.Month == month && u.Year == year);

            var details = new List<InvoiceDetail>();

            // 1) Tiền thuê phòng
            var rentAmount = contract.MonthlyRent;
            details.Add(new InvoiceDetail { ItemName = "Tiền thuê phòng", Quantity = 1, UnitPrice = rentAmount, Amount = rentAmount });

            // 2) Điện - Nước (theo chỉ số đã ghi)
            var electricAmount = utility?.ElectricAmount ?? 0;
            var waterAmount = utility?.WaterAmount ?? 0;
            if (utility != null)
            {
                if (utility.ElectricUsage != 0)
                    details.Add(new InvoiceDetail { ItemName = $"Tiền điện ({utility.ElectricUsage:N0} kWh)", Quantity = utility.ElectricUsage, UnitPrice = utility.ElectricPrice, Amount = electricAmount });
                if (utility.WaterUsage != 0)
                    details.Add(new InvoiceDetail { ItemName = $"Tiền nước ({utility.WaterUsage:N0} m³)", Quantity = utility.WaterUsage, UnitPrice = utility.WaterPrice, Amount = waterAmount });
            }

            // 3) Dịch vụ định kỳ đã đăng ký cho hợp đồng
            decimal serviceAmount = 0;
            foreach (var sub in contract.Subscriptions)
            {
                var qty = sub.IsPerPerson ? sub.Quantity * Math.Max(1, contract.OccupantCount) : sub.Quantity;
                var amount = qty * sub.UnitPrice;
                serviceAmount += amount;
                details.Add(new InvoiceDetail
                {
                    ServiceId = sub.ServiceId,
                    ItemName = sub.Service?.ServiceName ?? "Dịch vụ",
                    Quantity = qty,
                    UnitPrice = sub.UnitPrice,
                    Amount = amount
                });
            }

            // 4) Công nợ dồn từ các kỳ trước (chưa thu hết, chưa hủy)
            var previousDebt = (await _uow.Invoices.Query()
                .Where(i => i.RoomId == contract.RoomId
                         && i.Status != InvoiceStatus.Paid
                         && i.Status != InvoiceStatus.Cancelled
                         && (i.Year < year || (i.Year == year && i.Month < month)))
                .Select(i => i.TotalAmount - i.PaidAmount)
                .ToListAsync()).Sum();

            // 5) Phí phạt trễ hạn trên công nợ kỳ trước (nếu có cấu hình)
            if (setting.LateFeePercent > 0 && previousDebt > 0)
            {
                var fee = Math.Round(previousDebt * setting.LateFeePercent / 100m, 0, MidpointRounding.AwayFromZero);
                if (fee > 0)
                {
                    serviceAmount += fee;
                    details.Add(new InvoiceDetail
                    {
                        ItemName = $"Phí trễ hạn ({setting.LateFeePercent:0.##}% nợ cũ)",
                        Quantity = 1, UnitPrice = fee, Amount = fee
                    });
                }
            }

            var invoice = new Invoice
            {
                InvoiceNo = $"HD{year:D4}{month:D2}{(++seq):D4}",
                RoomId = contract.RoomId,
                TenantId = contract.TenantId,
                Month = month,
                Year = year,
                InvoiceDate = new DateTime(year, month, 1),
                DueDate = new DateTime(year, month, Math.Min(contract.PaymentDueDay, daysInMonth)),
                RentAmount = rentAmount,
                ElectricAmount = electricAmount,
                WaterAmount = waterAmount,
                ServiceAmount = serviceAmount,
                PreviousDebt = previousDebt,
                Status = InvoiceStatus.Unpaid,
                InvoiceDetails = details
            };
            invoice.TotalAmount = Math.Round(rentAmount + electricAmount + waterAmount + serviceAmount - invoice.DiscountAmount + previousDebt, 0, MidpointRounding.AwayFromZero);
            await _uow.Invoices.AddAsync(invoice);
        }
        await _uow.SaveChangesAsync();
        await _audit.LogAsync("Tạo hóa đơn tháng", "Hóa đơn", $"{month:D2}/{year}", $"Đã tạo {seq - startSeq} hóa đơn");
    }

    public async Task<int> MarkOverdueAsync()
    {
        try
        {
            var today = DateTime.Today;
            var overdue = await _uow.Invoices.Query()
                .Where(i => (i.Status == InvoiceStatus.Unpaid || i.Status == InvoiceStatus.PartiallyPaid)
                         && i.DueDate != null && i.DueDate < today
                         && i.PaidAmount < i.TotalAmount)
                .ToListAsync();
            foreach (var inv in overdue)
            {
                inv.Status = InvoiceStatus.Overdue;
                _uow.Invoices.Update(inv);
            }
            if (overdue.Count > 0) await _uow.SaveChangesAsync();
            return overdue.Count;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking overdue invoices");
            return 0;
        }
    }

    public async Task<decimal> GetTotalUnpaidAsync() =>
        (await _uow.Invoices.Query()
            .Where(i => i.Status == InvoiceStatus.Unpaid || i.Status == InvoiceStatus.Overdue || i.Status == InvoiceStatus.PartiallyPaid)
            .Select(i => i.TotalAmount - i.PaidAmount)
            .ToListAsync()).Sum();

    public async Task<decimal> GetRevenueByMonthAsync(int month, int year) =>
        // Thực thu = tổng tiền đã thu của các hóa đơn trong kỳ (gồm cả hóa đơn trả một phần)
        (await _uow.Invoices.Query()
            .Where(i => i.Month == month && i.Year == year)
            .Select(i => i.PaidAmount)
            .ToListAsync()).Sum();
}
