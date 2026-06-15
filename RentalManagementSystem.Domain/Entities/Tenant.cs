namespace RentalManagementSystem.Domain.Entities;

public class Tenant : BaseEntity
{
    public string FullName { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? CCCD { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public string? Address { get; set; }
    public string? Occupation { get; set; }
    public string? AvatarPath { get; set; }
    public string? Note { get; set; }
    public ICollection<Contract> Contracts { get; set; } = new List<Contract>();
    public ICollection<Invoice> Invoices { get; set; } = new List<Invoice>();
    public ICollection<Vehicle> Vehicles { get; set; } = new List<Vehicle>();
}
