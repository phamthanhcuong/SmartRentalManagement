namespace RentalManagementSystem.Domain.Entities;

public class RentalArea : BaseEntity
{
    public string AreaCode { get; set; } = string.Empty;
    public string AreaName { get; set; } = string.Empty;
    public string? Address { get; set; }
    public string? Description { get; set; }
    public string? OwnerName { get; set; }
    public string? OwnerPhone { get; set; }

    // Đơn giá mặc định cấu hình theo khu (áp dụng khi nhập chỉ số điện/nước)
    public decimal ElectricPrice { get; set; } = 3500;
    public decimal WaterPrice { get; set; } = 15000;

    public ICollection<Room> Rooms { get; set; } = new List<Room>();
}
