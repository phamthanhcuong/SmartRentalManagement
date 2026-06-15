using RentalManagementSystem.Domain.Entities;
using RentalManagementSystem.Domain.Enums;

namespace RentalManagementSystem.Application.Interfaces;

public interface IUserService
{
    Task<User?> LoginAsync(string username, string password);
    Task<User?> GetByIdAsync(int id);
    Task<IEnumerable<User>> GetAllAsync();
    Task<bool> CreateAsync(User user, string password);
    Task<bool> UpdateAsync(User user);
    Task<bool> ChangePasswordAsync(int userId, string oldPassword, string newPassword);
    Task<bool> DeleteAsync(int id);
}

public interface IRentalAreaService
{
    Task<IEnumerable<RentalArea>> GetAllAsync();
    Task<RentalArea?> GetByIdAsync(int id);
    Task<bool> CreateAsync(RentalArea area);
    Task<bool> UpdateAsync(RentalArea area);
    Task<bool> DeleteAsync(int id);
}

public interface IRoomService
{
    Task<IEnumerable<Room>> GetAllAsync();
    Task<IEnumerable<Room>> GetByAreaAsync(int areaId);
    Task<Room?> GetByIdAsync(int id);
    Task<IEnumerable<Room>> SearchAsync(string keyword);
    Task<bool> CreateAsync(Room room);
    Task<bool> UpdateAsync(Room room);
    Task<bool> DeleteAsync(int id);
    Task<int> GetAvailableCountAsync();
    Task<int> GetOccupiedCountAsync();
}

public interface ITenantService
{
    Task<IEnumerable<Tenant>> GetAllAsync();
    Task<Tenant?> GetByIdAsync(int id);
    Task<IEnumerable<Tenant>> SearchAsync(string keyword);
    Task<bool> CreateAsync(Tenant tenant);
    Task<bool> UpdateAsync(Tenant tenant);
    Task<bool> DeleteAsync(int id);
}

public interface IContractService
{
    Task<IEnumerable<Contract>> GetAllAsync();
    Task<Contract?> GetByIdAsync(int id);
    Task<Contract?> GetActiveByRoomAsync(int roomId);
    Task<bool> CreateAsync(Contract contract);
    Task<bool> UpdateAsync(Contract contract);
    /// <summary>Thanh lý hợp đồng, hoàn cọc sau khi trừ khoản khấu trừ (hư hỏng, nợ...).</summary>
    Task<bool> TerminateAsync(int id, decimal depositDeduction = 0);
    Task<bool> RenewAsync(int id, DateTime newEndDate);
    Task<string> GenerateContractNoAsync();
    /// <summary>Tự động chuyển hợp đồng quá hạn sang trạng thái Hết hạn. Trả về số bản ghi cập nhật.</summary>
    Task<int> UpdateExpiredStatusesAsync();
    /// <summary>Tổng công nợ chưa thu của 1 hợp đồng (dùng đối soát khi trả phòng).</summary>
    Task<decimal> GetOutstandingByContractAsync(int contractId);
    /// <summary>Trả phòng: thanh lý HĐ, hoàn cọc sau khấu trừ, ghi ngày trả phòng, phòng về trạng thái trống.</summary>
    Task<bool> CheckoutAsync(int id, decimal depositDeduction = 0);
    /// <summary>Nhường phòng: kết thúc HĐ cũ và tạo HĐ mới cho khách khác trên cùng phòng.</summary>
    Task<bool> TransferAsync(int oldContractId, int newTenantId, decimal newDeposit, bool keepDeposit);
}

/// <summary>Người dùng đang đăng nhập (UI cung cấp) — để gắn vào nhật ký thao tác.</summary>
public interface ICurrentUser
{
    string UserName { get; }
}

public interface IAuditService
{
    Task LogAsync(string action, string entityType, string? entityRef = null, string? details = null);
    Task<IEnumerable<AuditLog>> GetRecentAsync(int take = 500);
}

public interface ISystemSettingService
{
    Task<SystemSetting> GetAsync();
    Task<bool> UpdateAsync(SystemSetting setting);
}

public interface IBackupService
{
    /// <summary>Sao lưu CSDL ra file (mặc định vào thư mục Sao lưu, tên kèm thời gian). Trả về đường dẫn file.</summary>
    Task<string> BackupAsync(string? destinationPath = null);
    /// <summary>Phục hồi CSDL từ file sao lưu (ghi đè dữ liệu hiện tại). Cần khởi động lại app sau đó.</summary>
    void Restore(string sourcePath);
    /// <summary>Đường dẫn file CSDL đang dùng.</summary>
    string GetDatabasePath();
}

public interface IVehicleService
{
    Task<IEnumerable<Vehicle>> GetByTenantAsync(int tenantId);
    Task<IEnumerable<Vehicle>> GetAllActiveAsync();
    Task<bool> AddAsync(Vehicle vehicle);
    Task<bool> UpdateAsync(Vehicle vehicle);
    Task<bool> RemoveAsync(int id);
    /// <summary>Tra cứu xe theo biển số / chủ xe / SĐT — trả kèm phòng &amp; khu đang ở.</summary>
    Task<IEnumerable<VehicleLookupItem>> SearchAsync(string? keyword);
}

/// <summary>Kết quả tra cứu xe (gắn chủ xe + phòng/khu hiện tại).</summary>
public record VehicleLookupItem(
    string LicensePlate, VehicleType VehicleType, string? Brand, string? Color,
    string OwnerName, string OwnerPhone, string RoomCode, string AreaName, DateTime RegisterDate
);

public interface IUtilityService
{
    Task<IEnumerable<UtilityReading>> GetByRoomAsync(int roomId);
    Task<UtilityReading?> GetByRoomMonthYearAsync(int roomId, int month, int year);
    /// <summary>Bản ghi gần nhất trước (tháng, năm) của phòng — dùng kế thừa chỉ số đầu kỳ.</summary>
    Task<UtilityReading?> GetPreviousReadingAsync(int roomId, int month, int year);
    /// <summary>Lấy danh sách nhập nhanh điện/nước cho mọi phòng đang thuê trong kỳ (đã điền sẵn chỉ số cũ &amp; đơn giá).</summary>
    Task<IEnumerable<UtilityBatchRow>> GetBatchEntryAsync(int month, int year);
    /// <summary>Lưu hàng loạt: tạo mới hoặc cập nhật chỉ số cho nhiều phòng cùng lúc. Trả về số bản ghi đã lưu.</summary>
    Task<int> SaveBatchAsync(int month, int year, IEnumerable<UtilityReading> readings);
    Task<bool> CreateAsync(UtilityReading reading);
    Task<bool> UpdateAsync(UtilityReading reading);
    Task<bool> DeleteAsync(int id);
    Task<IEnumerable<UtilityReading>> GetByMonthYearAsync(int month, int year);
}

public interface IContractSubscriptionService
{
    Task<IEnumerable<ContractSubscription>> GetByContractAsync(int contractId);
    Task<bool> AddAsync(ContractSubscription subscription);
    Task<bool> RemoveAsync(int id);
}

public interface IServiceService
{
    Task<IEnumerable<Domain.Entities.Service>> GetAllAsync();
    Task<Domain.Entities.Service?> GetByIdAsync(int id);
    Task<bool> CreateAsync(Domain.Entities.Service service);
    Task<bool> UpdateAsync(Domain.Entities.Service service);
    Task<bool> DeleteAsync(int id);
}

public interface IInvoiceService
{
    Task<IEnumerable<Invoice>> GetAllAsync();
    Task<Invoice?> GetByIdAsync(int id);
    Task<IEnumerable<Invoice>> GetByRoomAsync(int roomId);
    Task<IEnumerable<Invoice>> GetByMonthYearAsync(int month, int year);
    Task<bool> CreateAsync(Invoice invoice);
    Task<bool> UpdateAsync(Invoice invoice);
    Task<bool> PayAsync(int id, decimal amount);
    Task<bool> DeleteAsync(int id);
    Task<string> GenerateInvoiceNoAsync();
    Task GenerateMonthlyInvoicesAsync(int month, int year);
    /// <summary>Đánh dấu Quá hạn cho hóa đơn chưa thu đủ và đã qua hạn thanh toán.</summary>
    Task<int> MarkOverdueAsync();
    Task<decimal> GetTotalUnpaidAsync();
    Task<decimal> GetRevenueByMonthAsync(int month, int year);
}

public interface IIncomeExpenseService
{
    Task<IEnumerable<IncomeExpense>> GetAllAsync();
    Task<IEnumerable<IncomeExpense>> GetByTypeAsync(TransactionType type);
    Task<IEnumerable<IncomeExpense>> GetByMonthYearAsync(int month, int year);
    Task<bool> CreateAsync(IncomeExpense item);
    Task<bool> UpdateAsync(IncomeExpense item);
    Task<bool> DeleteAsync(int id);
    Task<decimal> GetTotalIncomeAsync(int month, int year);
    Task<decimal> GetTotalExpenseAsync(int month, int year);
}

public interface IDashboardService
{
    Task<DashboardStats> GetStatsAsync();
    Task<IEnumerable<MonthlyRevenue>> GetMonthlyRevenueAsync(int year);
    Task<IEnumerable<RoomOccupancyRate>> GetOccupancyRateAsync();
    /// <summary>Cảnh báo hành động cần xử lý ngay (kỳ hiện tại).</summary>
    Task<DashboardAlerts> GetAlertsAsync();
}

/// <summary>Các cảnh báo hành động hiển thị trên Dashboard.</summary>
public record DashboardAlerts(
    int RoomsMissingReading,   // phòng đang thuê chưa nhập chỉ số tháng này
    int OverdueInvoices,       // số hóa đơn quá hạn
    decimal OverdueAmount,     // tổng tiền quá hạn
    int ExpiringContracts,     // HĐ sắp hết hạn (<=30 ngày)
    int RoomsNoInvoice         // phòng đang thuê chưa có hóa đơn tháng này
);

public record DashboardStats(
    int TotalRooms,
    int AvailableRooms,
    int OccupiedRooms,
    int TotalTenants,
    decimal TotalUnpaid,
    decimal MonthlyRevenue,
    int ExpiringContracts
);

public record MonthlyRevenue(int Month, decimal Revenue, decimal Expense);
public record RoomOccupancyRate(string AreaName, int Total, int Occupied);

/// <summary>Một dòng nhập nhanh điện/nước cho phòng đang thuê.</summary>
public record UtilityBatchRow(
    int RoomId, string RoomCode, string AreaName,
    decimal ElectricOld, decimal WaterOld,
    decimal ElectricNew, decimal WaterNew,
    decimal ElectricPrice, decimal WaterPrice,
    int? ExistingReadingId
);

public interface IAssetService
{
    Task<IEnumerable<Asset>> GetAllAsync();
    Task<Asset?> GetByIdAsync(int id);
    Task<bool> CreateAsync(Asset asset);
    Task<bool> UpdateAsync(Asset asset);
    Task<bool> DeleteAsync(int id);
    Task<IEnumerable<RoomAsset>> GetRoomAssetsAsync(int roomId);
    Task<bool> AssignToRoomAsync(RoomAsset roomAsset);
    Task<bool> RemoveFromRoomAsync(int roomAssetId);
}

public interface IReportService
{
    Task ExportRevenueToExcelAsync(int month, int year, string filePath);
    Task ExportDebtToExcelAsync(string filePath);
    Task ExportUtilityToExcelAsync(int month, int year, string filePath);
    Task PrintInvoiceAsync(int invoiceId, string filePath);
}
