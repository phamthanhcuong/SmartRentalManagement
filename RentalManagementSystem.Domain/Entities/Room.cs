using RentalManagementSystem.Domain.Enums;

namespace RentalManagementSystem.Domain.Entities;

public class Room : BaseEntity
{
    public string RoomCode { get; set; } = string.Empty;
    public string RoomName { get; set; } = string.Empty;
    public int RentalAreaId { get; set; }
    public decimal Area { get; set; }
    public decimal Price { get; set; }
    public decimal Deposit { get; set; }
    public int MaxOccupants { get; set; } = 2;
    public RoomStatus Status { get; set; } = RoomStatus.Available;
    public string? Description { get; set; }
    public string? ImagePath { get; set; }
    public RentalArea RentalArea { get; set; } = null!;
    public ICollection<Contract> Contracts { get; set; } = new List<Contract>();
    public ICollection<UtilityReading> UtilityReadings { get; set; } = new List<UtilityReading>();
    public ICollection<Invoice> Invoices { get; set; } = new List<Invoice>();
    public ICollection<RoomAsset> RoomAssets { get; set; } = new List<RoomAsset>();
}
