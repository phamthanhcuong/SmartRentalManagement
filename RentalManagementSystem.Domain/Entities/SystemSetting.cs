namespace RentalManagementSystem.Domain.Entities;

/// <summary>Cấu hình toàn hệ thống (1 bản ghi duy nhất, Id = 1).</summary>
public class SystemSetting : BaseEntity
{
    // Thông tin chủ trọ — in lên hóa đơn
    public string CompanyName { get; set; } = "NHÀ TRỌ";
    public string? Address { get; set; }
    public string? Phone { get; set; }
    public string? BankName { get; set; }
    public string? BankAccount { get; set; }
    public string? InvoiceFooterNote { get; set; } = "Cảm ơn Quý khách!";

    // Đơn giá mặc định (áp dụng khi tạo khu/phòng mới)
    public decimal DefaultElectricPrice { get; set; } = 3500;
    public decimal DefaultWaterPrice { get; set; } = 15000;

    // Phí phạt trễ hạn (% trên số tiền còn nợ, để tham chiếu/áp dụng khi thu)
    public decimal LateFeePercent { get; set; } = 0;
}
