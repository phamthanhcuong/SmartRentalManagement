namespace RentalManagementSystem.Domain.Entities;

public class UtilityReading : BaseEntity
{
    public int RoomId { get; set; }
    public int Month { get; set; }
    public int Year { get; set; }
    public decimal ElectricOld { get; set; }
    public decimal ElectricNew { get; set; }
    public decimal WaterOld { get; set; }
    public decimal WaterNew { get; set; }
    public decimal ElectricPrice { get; set; } = 3500;
    public decimal WaterPrice { get; set; } = 15000;
    public decimal ElectricUsage => ElectricNew - ElectricOld;
    public decimal WaterUsage => WaterNew - WaterOld;
    public decimal ElectricAmount => (ElectricNew - ElectricOld) * ElectricPrice;
    public decimal WaterAmount => (WaterNew - WaterOld) * WaterPrice;
    public decimal TotalAmount => ElectricAmount + WaterAmount;
    public string? Note { get; set; }
    public Room Room { get; set; } = null!;
}
