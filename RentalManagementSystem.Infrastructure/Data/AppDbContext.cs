using Microsoft.EntityFrameworkCore;
using RentalManagementSystem.Domain.Entities;
using RentalManagementSystem.Domain.Enums;
using BCrypt.Net;

namespace RentalManagementSystem.Infrastructure.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<RentalArea> RentalAreas => Set<RentalArea>();
    public DbSet<Room> Rooms => Set<Room>();
    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<Contract> Contracts => Set<Contract>();
    public DbSet<UtilityReading> UtilityReadings => Set<UtilityReading>();
    public DbSet<Domain.Entities.Service> Services => Set<Domain.Entities.Service>();
    public DbSet<Invoice> Invoices => Set<Invoice>();
    public DbSet<InvoiceDetail> InvoiceDetails => Set<InvoiceDetail>();
    public DbSet<IncomeExpense> IncomeExpenses => Set<IncomeExpense>();
    public DbSet<Asset> Assets => Set<Asset>();
    public DbSet<RoomAsset> RoomAssets => Set<RoomAsset>();
    public DbSet<ContractSubscription> ContractSubscriptions => Set<ContractSubscription>();
    public DbSet<Vehicle> Vehicles => Set<Vehicle>();
    public DbSet<SystemSetting> SystemSettings => Set<SystemSetting>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Global query filter for soft delete
        modelBuilder.Entity<User>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<RentalArea>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<Room>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<Tenant>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<Contract>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<UtilityReading>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<Domain.Entities.Service>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<Invoice>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<InvoiceDetail>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<IncomeExpense>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<Asset>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<RoomAsset>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<ContractSubscription>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<Vehicle>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<SystemSetting>(e =>
        {
            e.Property(s => s.DefaultElectricPrice).HasPrecision(18, 2);
            e.Property(s => s.DefaultWaterPrice).HasPrecision(18, 2);
            e.Property(s => s.LateFeePercent).HasPrecision(9, 2);
        });
        modelBuilder.Entity<AuditLog>(e =>
        {
            e.Property(a => a.UserName).HasMaxLength(100);
            e.Property(a => a.Action).HasMaxLength(100);
            e.Property(a => a.EntityType).HasMaxLength(50);
            e.HasIndex(a => a.Timestamp);
        });

        // User
        modelBuilder.Entity<User>(e =>
        {
            e.HasIndex(u => u.Username).IsUnique().HasFilter("IsDeleted = 0");
            e.Property(u => u.Username).HasMaxLength(100).IsRequired();
            e.Property(u => u.FullName).HasMaxLength(200).IsRequired();
            e.Property(u => u.PasswordHash).IsRequired();
        });

        // RentalArea
        modelBuilder.Entity<RentalArea>(e =>
        {
            e.HasIndex(a => a.AreaCode).IsUnique().HasFilter("IsDeleted = 0");
            e.Property(a => a.AreaCode).HasMaxLength(50).IsRequired();
            e.Property(a => a.AreaName).HasMaxLength(200).IsRequired();
            e.Property(a => a.ElectricPrice).HasPrecision(18, 2);
            e.Property(a => a.WaterPrice).HasPrecision(18, 2);
        });

        // Room
        modelBuilder.Entity<Room>(e =>
        {
            e.HasIndex(r => r.RoomCode).IsUnique().HasFilter("IsDeleted = 0");
            e.Property(r => r.RoomCode).HasMaxLength(50).IsRequired();
            e.Property(r => r.Price).HasPrecision(18, 2);
            e.Property(r => r.Deposit).HasPrecision(18, 2);
            e.Property(r => r.Area).HasPrecision(10, 2);
            e.HasOne(r => r.RentalArea)
             .WithMany(a => a.Rooms)
             .HasForeignKey(r => r.RentalAreaId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        // Tenant
        modelBuilder.Entity<Tenant>(e =>
        {
            e.Property(t => t.FullName).HasMaxLength(200).IsRequired();
            e.Property(t => t.Phone).HasMaxLength(20).IsRequired();
            e.HasIndex(t => t.CCCD);
        });

        // Contract
        modelBuilder.Entity<Contract>(e =>
        {
            e.HasIndex(c => c.ContractNo).IsUnique().HasFilter("IsDeleted = 0");
            e.Property(c => c.MonthlyRent).HasPrecision(18, 2);
            e.Property(c => c.DepositAmount).HasPrecision(18, 2);
            e.HasOne(c => c.Tenant).WithMany(t => t.Contracts).HasForeignKey(c => c.TenantId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(c => c.Room).WithMany(r => r.Contracts).HasForeignKey(c => c.RoomId).OnDelete(DeleteBehavior.Restrict);
        });

        // UtilityReading
        modelBuilder.Entity<UtilityReading>(e =>
        {
            e.HasIndex(u => new { u.RoomId, u.Month, u.Year }).IsUnique().HasFilter("IsDeleted = 0");
            e.Property(u => u.ElectricPrice).HasPrecision(18, 2);
            e.Property(u => u.WaterPrice).HasPrecision(18, 2);
            e.Ignore(u => u.ElectricUsage);
            e.Ignore(u => u.WaterUsage);
            e.Ignore(u => u.ElectricAmount);
            e.Ignore(u => u.WaterAmount);
            e.Ignore(u => u.TotalAmount);
            e.HasOne(u => u.Room).WithMany(r => r.UtilityReadings).HasForeignKey(u => u.RoomId).OnDelete(DeleteBehavior.Restrict);
        });

        // Invoice
        modelBuilder.Entity<Invoice>(e =>
        {
            e.HasIndex(i => i.InvoiceNo).IsUnique().HasFilter("IsDeleted = 0");
            e.Property(i => i.TotalAmount).HasPrecision(18, 2);
            e.Property(i => i.PaidAmount).HasPrecision(18, 2);
            e.Property(i => i.RentAmount).HasPrecision(18, 2);
            e.Property(i => i.ElectricAmount).HasPrecision(18, 2);
            e.Property(i => i.WaterAmount).HasPrecision(18, 2);
            e.Property(i => i.ServiceAmount).HasPrecision(18, 2);
            e.Property(i => i.DiscountAmount).HasPrecision(18, 2);
            e.Property(i => i.PreviousDebt).HasPrecision(18, 2);
            e.Ignore(i => i.OutstandingAmount);
            e.HasOne(i => i.Room).WithMany(r => r.Invoices).HasForeignKey(i => i.RoomId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(i => i.Tenant).WithMany(t => t.Invoices).HasForeignKey(i => i.TenantId).OnDelete(DeleteBehavior.Restrict);
        });

        // Vehicle
        modelBuilder.Entity<Vehicle>(e =>
        {
            e.Property(v => v.LicensePlate).HasMaxLength(20).IsRequired();
            e.HasIndex(v => v.LicensePlate);
            e.HasOne(v => v.Tenant).WithMany(t => t.Vehicles).HasForeignKey(v => v.TenantId).OnDelete(DeleteBehavior.Cascade);
        });

        // ContractSubscription
        modelBuilder.Entity<ContractSubscription>(e =>
        {
            e.Property(s => s.Quantity).HasPrecision(18, 2);
            e.Property(s => s.UnitPrice).HasPrecision(18, 2);
            e.HasOne(s => s.Contract).WithMany(c => c.Subscriptions).HasForeignKey(s => s.ContractId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(s => s.Service).WithMany().HasForeignKey(s => s.ServiceId).OnDelete(DeleteBehavior.Restrict);
        });

        // InvoiceDetail
        modelBuilder.Entity<InvoiceDetail>(e =>
        {
            e.Property(d => d.Amount).HasPrecision(18, 2);
            e.Property(d => d.UnitPrice).HasPrecision(18, 2);
            e.HasOne(d => d.Invoice).WithMany(i => i.InvoiceDetails).HasForeignKey(d => d.InvoiceId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(d => d.Service).WithMany(s => s.InvoiceDetails).HasForeignKey(d => d.ServiceId).OnDelete(DeleteBehavior.SetNull);
        });

        // IncomeExpense
        modelBuilder.Entity<IncomeExpense>(e =>
        {
            e.Property(ie => ie.Amount).HasPrecision(18, 2);
            e.HasOne(ie => ie.Room).WithMany().HasForeignKey(ie => ie.RoomId).OnDelete(DeleteBehavior.SetNull);
        });

        // Seed Data
        SeedData(modelBuilder);
    }

    private static void SeedData(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>().HasData(new User
        {
            Id = 1,
            Username = "admin",
            // Hash cố định (deterministic) cho mật khẩu "Admin@123"
            PasswordHash = "$2a$11$rIjmFFd4Hs7KSKgu4pdFtOlwTqhybKw45wQ/Rd34vQG7NciG2k0wO",
            FullName = "Quản Trị Viên",
            Role = UserRole.Admin,
            IsActive = true,
            CreatedAt = new DateTime(2024, 1, 1)
        });

        modelBuilder.Entity<RentalArea>().HasData(new RentalArea
        {
            Id = 1,
            AreaCode = "KTX001",
            AreaName = "Khu Trọ Số 1",
            Address = "123 Đường ABC, Phường XYZ, TP.HCM",
            OwnerName = "Nguyễn Văn A",
            OwnerPhone = "0901234567",
            ElectricPrice = 3500,
            WaterPrice = 15000,
            CreatedAt = new DateTime(2024, 1, 1)
        });

        modelBuilder.Entity<SystemSetting>().HasData(new SystemSetting
        {
            Id = 1,
            CompanyName = "NHÀ TRỌ",
            InvoiceFooterNote = "Cảm ơn Quý khách!",
            DefaultElectricPrice = 3500,
            DefaultWaterPrice = 15000,
            LateFeePercent = 0,
            CreatedAt = new DateTime(2024, 1, 1)
        });

        modelBuilder.Entity<Domain.Entities.Service>().HasData(
            new Domain.Entities.Service { Id = 1, ServiceName = "Điện", ServiceType = ServiceType.Electric, UnitPrice = 3500, Unit = "kWh", IsActive = true, CreatedAt = new DateTime(2024, 1, 1) },
            new Domain.Entities.Service { Id = 2, ServiceName = "Nước", ServiceType = ServiceType.Water, UnitPrice = 15000, Unit = "m³", IsActive = true, CreatedAt = new DateTime(2024, 1, 1) },
            new Domain.Entities.Service { Id = 3, ServiceName = "Internet", ServiceType = ServiceType.Internet, UnitPrice = 100000, Unit = "tháng", IsActive = true, CreatedAt = new DateTime(2024, 1, 1) },
            new Domain.Entities.Service { Id = 4, ServiceName = "Giữ xe", ServiceType = ServiceType.Parking, UnitPrice = 50000, Unit = "tháng", IsActive = true, CreatedAt = new DateTime(2024, 1, 1) }
        );
    }
}
