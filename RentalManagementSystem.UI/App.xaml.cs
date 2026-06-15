using System.Windows;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using RentalManagementSystem.Application.Interfaces;
using RentalManagementSystem.Application.Services;
using RentalManagementSystem.Infrastructure.Data;
using RentalManagementSystem.Infrastructure.Repositories;
using RentalManagementSystem.UI.ViewModels;
using RentalManagementSystem.UI.Views;
using Serilog;

namespace RentalManagementSystem.UI;

public partial class App : System.Windows.Application
{
    private IHost _host = null!;

    protected override async void OnStartup(StartupEventArgs e)
    {
        Log.Logger = new LoggerConfiguration()
            .WriteTo.File("logs/app-.log", rollingInterval: RollingInterval.Day)
            .WriteTo.Debug()
            .CreateLogger();

        _host = Host.CreateDefaultBuilder()
            .UseSerilog()
            .ConfigureServices(ConfigureServices)
            .Build();

        await _host.StartAsync();

        // Ensure DB is created & migrated
        using var scope = _host.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        // Áp dụng migration: tạo DB nếu chưa có, nâng cấp schema khi cập nhật phần mềm
        // (giữ nguyên dữ liệu — thay cho EnsureCreated vốn không nâng cấp được).
        await db.Database.MigrateAsync();

        var loginWindow = _host.Services.GetRequiredService<LoginWindow>();
        loginWindow.Show();

        base.OnStartup(e);
    }

    private void ConfigureServices(IServiceCollection services)
    {
        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlite("Data Source=RentalDB.db"));

        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IBackupService, Infrastructure.Services.BackupService>();
        services.AddScoped<ISystemSettingService, SystemSettingService>();
        services.AddSingleton<ICurrentUser, Services.CurrentUser>();
        services.AddScoped<IAuditService, AuditService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IRentalAreaService, RentalAreaService>();
        services.AddScoped<IRoomService, RoomService>();
        services.AddScoped<IContractSubscriptionService, ContractSubscriptionService>();
        services.AddScoped<IVehicleService, VehicleService>();
        services.AddTransient<Services.InvoicePrintService>();
        services.AddScoped<ITenantService, TenantService>();
        services.AddScoped<IContractService, ContractService>();
        services.AddScoped<IUtilityService, UtilityService>();
        services.AddScoped<IServiceService, ServiceService>();
        services.AddScoped<IInvoiceService, InvoiceService>();
        services.AddScoped<IIncomeExpenseService, IncomeExpenseService>();
        services.AddScoped<IAssetService, AssetService>();
        services.AddScoped<IDashboardService, DashboardService>();
        services.AddScoped<IReportService, ReportService>();

        // ViewModels
        services.AddTransient<LoginViewModel>();
        services.AddTransient<MainViewModel>();
        services.AddTransient<HomeViewModel>();
        services.AddTransient<DashboardViewModel>();
        services.AddTransient<RoomViewModel>();
        services.AddTransient<TenantViewModel>();
        services.AddTransient<ContractViewModel>();
        services.AddTransient<UtilityViewModel>();
        services.AddTransient<InvoiceViewModel>();
        services.AddTransient<IncomeExpenseViewModel>();
        services.AddTransient<RentalAreaViewModel>();
        services.AddTransient<ServiceViewModel>();
        services.AddTransient<AssetViewModel>();
        services.AddTransient<VehicleLookupViewModel>();
        services.AddTransient<AuditLogViewModel>();
        services.AddTransient<ReportViewModel>();
        services.AddTransient<SettingsViewModel>();

        // Windows
        services.AddTransient<LoginWindow>();
        services.AddTransient<MainWindow>();
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        await _host.StopAsync();
        _host.Dispose();
        Log.CloseAndFlush();
        base.OnExit(e);
    }

    public static T GetService<T>() where T : class =>
        (Current as App)!._host.Services.GetRequiredService<T>();
}
