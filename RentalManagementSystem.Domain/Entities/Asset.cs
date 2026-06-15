namespace RentalManagementSystem.Domain.Entities;

public class Asset : BaseEntity
{
    public string AssetCode { get; set; } = string.Empty;
    public string AssetName { get; set; } = string.Empty;
    public string? Category { get; set; }
    public decimal PurchasePrice { get; set; }
    public DateTime? PurchaseDate { get; set; }
    public string? Condition { get; set; }
    public string? Description { get; set; }
    public ICollection<RoomAsset> RoomAssets { get; set; } = new List<RoomAsset>();
}
