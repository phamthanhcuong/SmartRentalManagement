using RentalManagementSystem.Domain.Enums;

namespace RentalManagementSystem.Domain.Entities;

public class Invoice : BaseEntity
{
    public string InvoiceNo { get; set; } = string.Empty;
    public int RoomId { get; set; }
    public int TenantId { get; set; }
    public int Month { get; set; }
    public int Year { get; set; }
    public DateTime InvoiceDate { get; set; } = DateTime.Now;
    public DateTime? DueDate { get; set; }
    public DateTime? PaidDate { get; set; }
    public decimal RentAmount { get; set; }
    public decimal ElectricAmount { get; set; }
    public decimal WaterAmount { get; set; }
    public decimal ServiceAmount { get; set; }
    public decimal DiscountAmount { get; set; }
    /// <summary>Công nợ chưa thanh toán dồn từ các kỳ trước.</summary>
    public decimal PreviousDebt { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal PaidAmount { get; set; }
    public InvoiceStatus Status { get; set; } = InvoiceStatus.Unpaid;
    public string? Note { get; set; }
    public Room Room { get; set; } = null!;
    public Tenant Tenant { get; set; } = null!;
    public ICollection<InvoiceDetail> InvoiceDetails { get; set; } = new List<InvoiceDetail>();

    /// <summary>Số tiền còn phải thu = Tổng - Đã thu.</summary>
    public decimal OutstandingAmount => TotalAmount - PaidAmount;
}
