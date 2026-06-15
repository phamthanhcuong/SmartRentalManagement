using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RentalManagementSystem.Application.Interfaces;
using RentalManagementSystem.Infrastructure.Data;

namespace RentalManagementSystem.Infrastructure.Services;

public class BackupService : IBackupService
{
    private readonly AppDbContext _db;
    private readonly ILogger<BackupService> _logger;

    public BackupService(AppDbContext db, ILogger<BackupService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public string GetDatabasePath()
    {
        var conn = _db.Database.GetDbConnection();
        // DataSource trả về đường dẫn file SQLite (tuyệt đối nếu provider phân giải được)
        var path = conn.DataSource;
        return Path.IsPathRooted(path) ? path : Path.GetFullPath(path);
    }

    public async Task<string> BackupAsync(string? destinationPath = null)
    {
        if (string.IsNullOrWhiteSpace(destinationPath))
        {
            var dir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                "QuanLyNhaTro", "SaoLuu");
            Directory.CreateDirectory(dir);
            destinationPath = Path.Combine(dir, $"NhaTro_{DateTime.Now:yyyyMMdd_HHmmss}.db");
        }

        // VACUUM INTO tạo bản sao sạch, an toàn ngay cả khi DB đang mở
        var safe = destinationPath.Replace("'", "''");
        await _db.Database.ExecuteSqlRawAsync($"VACUUM INTO '{safe}'");
        _logger.LogInformation("Đã sao lưu CSDL ra {Path}", destinationPath);
        return destinationPath;
    }

    public void Restore(string sourcePath)
    {
        if (!File.Exists(sourcePath))
            throw new FileNotFoundException("Không tìm thấy file sao lưu.", sourcePath);

        var dbPath = GetDatabasePath();

        // Đóng mọi kết nối SQLite đang gộp để giải phóng file
        SqliteConnection.ClearAllPools();
        _db.Database.GetDbConnection().Close();

        // Sao lưu nhanh file hiện tại trước khi ghi đè (đề phòng phục hồi nhầm)
        var preRestore = dbPath + ".before_restore";
        try { File.Copy(dbPath, preRestore, true); } catch { /* bỏ qua nếu không sao chép được */ }

        File.Copy(sourcePath, dbPath, true);
        // Xóa các file phụ WAL/SHM để tránh lệch dữ liệu sau khi ghi đè
        foreach (var suffix in new[] { "-wal", "-shm" })
        {
            var extra = dbPath + suffix;
            if (File.Exists(extra)) { try { File.Delete(extra); } catch { } }
        }
        _logger.LogInformation("Đã phục hồi CSDL từ {Path}", sourcePath);
    }
}
