using RentalManagementSystem.Domain.Enums;

namespace RentalManagementSystem.Domain.Entities;

/// <summary>Phương tiện của khách thuê — phục vụ phí gửi xe và quản lý an ninh.</summary>
public class Vehicle : BaseEntity
{
    public int TenantId { get; set; }
    public string LicensePlate { get; set; } = string.Empty;
    public VehicleType VehicleType { get; set; } = VehicleType.Motorbike;
    public string? Brand { get; set; }
    public string? Color { get; set; }
    public DateTime RegisterDate { get; set; } = DateTime.Now;
    public bool IsActive { get; set; } = true;
    public string? Note { get; set; }
    public Tenant Tenant { get; set; } = null!;
}
