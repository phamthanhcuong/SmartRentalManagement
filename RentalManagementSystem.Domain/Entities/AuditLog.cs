namespace RentalManagementSystem.Domain.Entities;

/// <summary>Nhật ký thao tác — ghi lại ai làm gì, khi nào (phục vụ truy vết, tranh chấp).</summary>
public class AuditLog : BaseEntity
{
    public string UserName { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;       // vd: "Tạo hóa đơn", "Xóa hóa đơn", "Thu tiền"
    public string EntityType { get; set; } = string.Empty;   // vd: "Hóa đơn", "Hợp đồng"
    public string? EntityRef { get; set; }                   // mã/định danh đối tượng
    public string? Details { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.Now;
}
