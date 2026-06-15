using RentalManagementSystem.Domain.Enums;

namespace RentalManagementSystem.Domain.Entities;

public class Service : BaseEntity
{
    public string ServiceName { get; set; } = string.Empty;
    public ServiceType ServiceType { get; set; }
    public decimal UnitPrice { get; set; }
    public string? Unit { get; set; }
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
    public ICollection<InvoiceDetail> InvoiceDetails { get; set; } = new List<InvoiceDetail>();
}
