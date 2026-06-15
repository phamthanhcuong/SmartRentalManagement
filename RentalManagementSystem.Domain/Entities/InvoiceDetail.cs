namespace RentalManagementSystem.Domain.Entities;

public class InvoiceDetail : BaseEntity
{
    public int InvoiceId { get; set; }
    public int? ServiceId { get; set; }
    public string ItemName { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal Amount { get; set; }
    public string? Note { get; set; }
    public Invoice Invoice { get; set; } = null!;
    public Service? Service { get; set; }
}
