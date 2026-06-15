namespace RentalManagementSystem.Domain.Entities;

public class RoomAsset : BaseEntity
{
    public int RoomId { get; set; }
    public int AssetId { get; set; }
    public int Quantity { get; set; } = 1;
    public DateTime AssignedDate { get; set; } = DateTime.Now;
    public string? Note { get; set; }
    public Room Room { get; set; } = null!;
    public Asset Asset { get; set; } = null!;
}
