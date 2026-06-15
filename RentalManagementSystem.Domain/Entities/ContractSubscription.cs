namespace RentalManagementSystem.Domain.Entities;

/// <summary>
/// Dịch vụ định kỳ mà một hợp đồng đăng ký sử dụng (rác, internet, gửi xe...).
/// Cho phép tùy chỉnh đơn giá và số lượng riêng cho từng phòng.
/// </summary>
public class ContractSubscription : BaseEntity
{
    public int ContractId { get; set; }
    public int ServiceId { get; set; }

    /// <summary>Số lượng. Nếu tính theo đầu người sẽ nhân với số người ở.</summary>
    public decimal Quantity { get; set; } = 1;

    /// <summary>Đơn giá áp dụng (mặc định lấy từ Service.UnitPrice nhưng có thể chỉnh).</summary>
    public decimal UnitPrice { get; set; }

    /// <summary>True: phí nhân theo số người ở trong phòng (vd: tiền rác/người).</summary>
    public bool IsPerPerson { get; set; }

    public Contract Contract { get; set; } = null!;
    public Service Service { get; set; } = null!;
}
