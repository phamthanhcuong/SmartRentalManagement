using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace RentalManagementSystem.Infrastructure.Data;

/// <summary>
/// Cho phép công cụ "dotnet ef" tạo DbContext lúc thiết kế (tạo/áp migration)
/// mà không cần khởi động ứng dụng WPF.
/// </summary>
public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite("Data Source=RentalDB.db")
            .Options;
        return new AppDbContext(options);
    }
}
