using RentalManagementSystem.Domain.Enums;

namespace RentalManagementSystem.Domain.Entities;

public class IncomeExpense : BaseEntity
{
    public TransactionType Type { get; set; }
    public string Category { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Description { get; set; } = string.Empty;
    public DateTime TransactionDate { get; set; } = DateTime.Now;
    public int? RoomId { get; set; }
    public int? InvoiceId { get; set; }
    public string? Reference { get; set; }
    /// <summary>
    /// True: giao dịch tiền cọc (nhận/hoàn). Cọc là khoản phải trả lại, KHÔNG tính vào
    /// doanh thu/chi phí (lãi-lỗ); chỉ phản ánh trên dòng tiền/sổ quỹ.
    /// </summary>
    public bool IsDeposit { get; set; }
    public Room? Room { get; set; }
}
