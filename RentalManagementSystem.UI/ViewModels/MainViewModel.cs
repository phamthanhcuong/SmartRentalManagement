using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using RentalManagementSystem.Domain.Entities;

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
    private string _elderModeLabel = "Chế độ Người Cao Tuổi";

    partial void OnIsElderModeChanged(bool value)
        => ElderModeLabel = value ? "Chế độ Thường" : "Chế độ Người Cao Tuổi";

    [RelayCommand]
    private void ToggleElderMode() => IsElderMode = !IsElderMode;

    /// <summary>Cho phép các ViewModel con yêu cầu điều hướng tới một menu (vd: Dashboard → Điện Nước).</summary>
    public static Action<string>? NavigateRequested { get; private set; }

    public MainViewModel(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
        CurrentUser = LoginViewModel.CurrentUser;
        NavigateRequested = menu => NavigateTo(menu);
        NavigateTo("TrangChu");
    }

    [RelayCommand]
    private void NavigateTo(string menu)
    {
        ActiveMenu = menu;
        CurrentPageTitle = menu switch
        {
            "TrangChu" => "Trang Chủ",
            "Dashboard" => "Dashboard",
            "KhuTro" => "Khu Trọ",
            "Phong" => "Quản Lý Phòng",
            "KhachThue" => "Khách Thuê",
            "TraCuuXe" => "Tra Cứu Xe",
            "HopDong" => "Hợp Đồng",
            "DienNuoc" => "Điện Nước",
            "DichVu" => "Dịch Vụ",
            "HoaDon" => "Hóa Đơn",
            "ThuChi" => "Thu Chi",
            "TaiSan" => "Tài Sản",
            "BaoCao" => "Báo Cáo",
            "NhatKy" => "Nhật Ký Thao Tác",
            "CaiDat" => "Cài Đặt",
            _ => menu
        };

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
