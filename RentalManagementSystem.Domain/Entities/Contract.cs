using RentalManagementSystem.Domain.Enums;

namespace RentalManagementSystem.Domain.Entities;

public class Contract : BaseEntity
{
    public string ContractNo { get; set; } = string.Empty;
    public int TenantId { get; set; }
    public int RoomId { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public decimal DepositAmount { get; set; }
    public decimal MonthlyRent { get; set; }
    public int PaymentDueDay { get; set; } = 5;
    /// <summary>Số người ở thực tế trong phòng (dùng tính phí theo đầu người).</summary>
    public int OccupantCount { get; set; } = 1;
    public ContractStatus Status { get; set; } = ContractStatus.Active;
    /// <summary>Ngày khách trả phòng / kết thúc thực tế.</summary>
    public DateTime? CheckoutDate { get; set; }
    public string? Terms { get; set; }
    public string? Note { get; set; }
    public Tenant Tenant { get; set; } = null!;
    public Room Room { get; set; } = null!;
    public ICollection<ContractSubscription> Subscriptions { get; set; } = new List<ContractSubscription>();
}
