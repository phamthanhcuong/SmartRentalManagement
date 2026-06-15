using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using RentalManagementSystem.Domain.Entities;
using RentalManagementSystem.UI.Localization;

namespace RentalManagementSystem.UI.ViewModels;

public partial class MainViewModel : BaseViewModel
{
    private readonly IServiceProvider _serviceProvider;

    [ObservableProperty]
    private BaseViewModel _currentViewModel = null!;

    [ObservableProperty]
    private string _currentPageTitle = "Dashboard";

    [ObservableProperty]
    private string _activeMenu = "Dashboard";

    [ObservableProperty]
    private User? _currentUser;

    /// <summary>Chế độ Người Cao Tuổi: phóng to toàn bộ chữ &amp; nút.</summary>
    [ObservableProperty]
    private bool _isElderMode;

    [ObservableProperty]
    private string _elderModeLabel = string.Empty;

    partial void OnIsElderModeChanged(bool value) => UpdateElderLabel();
    private void UpdateElderLabel() => ElderModeLabel = Localizer.Instance[IsElderMode ? "Top.ElderOff" : "Top.ElderOn"];

    [RelayCommand]
    private void ToggleElderMode() => IsElderMode = !IsElderMode;

    /// <summary>Cho phép các ViewModel con yêu cầu điều hướng tới một menu (vd: Dashboard → Điện Nước).</summary>
    public static Action<string>? NavigateRequested { get; private set; }

    // menu -> khóa dịch tiêu đề
    private static string TitleKey(string menu) => menu switch
    {
        "TrangChu" => "Menu.Home", "Dashboard" => "Menu.Dashboard", "KhuTro" => "Menu.Area",
        "Phong" => "Menu.Room", "KhachThue" => "Menu.Tenant", "TraCuuXe" => "Menu.VehicleLookup",
        "HopDong" => "Menu.Contract", "DienNuoc" => "Menu.Utility", "DichVu" => "Menu.Service",
        "HoaDon" => "Menu.Invoice", "ThuChi" => "Menu.IncomeExpense", "TaiSan" => "Menu.Asset",
        "BaoCao" => "Menu.Report", "NhatKy" => "Menu.Audit", "CaiDat" => "Menu.Settings",
        _ => "Menu.Home"
    };

    public MainViewModel(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
        CurrentUser = LoginViewModel.CurrentUser;
        NavigateRequested = menu => NavigateTo(menu);
        UpdateElderLabel();
        // Cập nhật lại các chuỗi sinh từ C# khi đổi ngôn ngữ
        Localizer.Instance.LanguageChanged += () =>
        {
            CurrentPageTitle = Localizer.Instance[TitleKey(ActiveMenu)];
            UpdateElderLabel();
        };
        NavigateTo("TrangChu");
    }

    [RelayCommand]
    private void NavigateTo(string menu)
    {
        ActiveMenu = menu;
        CurrentPageTitle = Localizer.Instance[TitleKey(menu)];

        CurrentViewModel = menu switch
        {
            "TrangChu" => _serviceProvider.GetRequiredService<HomeViewModel>(),
            "Dashboard" => _serviceProvider.GetRequiredService<DashboardViewModel>(),
            "KhuTro" => _serviceProvider.GetRequiredService<RentalAreaViewModel>(),
            "Phong" => _serviceProvider.GetRequiredService<RoomViewModel>(),
            "KhachThue" => _serviceProvider.GetRequiredService<TenantViewModel>(),
            "TraCuuXe" => _serviceProvider.GetRequiredService<VehicleLookupViewModel>(),
            "HopDong" => _serviceProvider.GetRequiredService<ContractViewModel>(),
            "DienNuoc" => _serviceProvider.GetRequiredService<UtilityViewModel>(),
            "DichVu" => _serviceProvider.GetRequiredService<ServiceViewModel>(),
            "HoaDon" => _serviceProvider.GetRequiredService<InvoiceViewModel>(),
            "ThuChi" => _serviceProvider.GetRequiredService<IncomeExpenseViewModel>(),
            "TaiSan" => _serviceProvider.GetRequiredService<AssetViewModel>(),
            "BaoCao" => _serviceProvider.GetRequiredService<ReportViewModel>(),
            "NhatKy" => _serviceProvider.GetRequiredService<AuditLogViewModel>(),
            "CaiDat" => _serviceProvider.GetRequiredService<SettingsViewModel>(),
            _ => _serviceProvider.GetRequiredService<DashboardViewModel>()
        };

        _ = CurrentViewModel.LoadAsync();
    }

    [RelayCommand]
    private void Logout()
    {
        var loginWindow = _serviceProvider.GetRequiredService<Views.LoginWindow>();
        loginWindow.Show();
        foreach (System.Windows.Window window in System.Windows.Application.Current.Windows)
        {
            if (window is Views.MainWindow) { window.Close(); break; }
        }
    }
}
