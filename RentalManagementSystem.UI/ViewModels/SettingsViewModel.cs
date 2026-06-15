using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using RentalManagementSystem.Application.Interfaces;

namespace RentalManagementSystem.UI.ViewModels;

public partial class SettingsViewModel : BaseViewModel
{
    private readonly IUserService _userService;
    private readonly IBackupService _backupService;
    private readonly ISystemSettingService _settingService;
    private readonly ILogger<SettingsViewModel> _logger;

    private RentalManagementSystem.Domain.Entities.SystemSetting _setting = new();

    [ObservableProperty] private string _companyName = string.Empty;
    [ObservableProperty] private string _companyAddress = string.Empty;
    [ObservableProperty] private string _companyPhone = string.Empty;
    [ObservableProperty] private string _bankName = string.Empty;
    [ObservableProperty] private string _bankAccount = string.Empty;
    [ObservableProperty] private string _invoiceFooter = string.Empty;
    [ObservableProperty] private decimal _defaultElectricPrice;
    [ObservableProperty] private decimal _defaultWaterPrice;
    [ObservableProperty] private decimal _lateFeePercent;

    // Ngôn ngữ
    public IReadOnlyList<Localization.LanguageOption> Languages => Localization.Localizer.Instance.Languages;
    [ObservableProperty] private string _selectedLanguageCode = Localization.Localizer.Instance.CurrentLanguage;

    partial void OnSelectedLanguageCodeChanged(string value)
    {
        if (!string.IsNullOrEmpty(value)) Localization.Localizer.Instance.SetLanguage(value);
    }

    [ObservableProperty] private string _oldPassword = string.Empty;
    [ObservableProperty] private string _newPassword = string.Empty;
    [ObservableProperty] private string _confirmPassword = string.Empty;
    [ObservableProperty] private string _appVersion = "1.0.0";
    [ObservableProperty] private string _dbPath = "RentalDB.db";

    public SettingsViewModel(IUserService userService, IBackupService backupService,
        ISystemSettingService settingService, ILogger<SettingsViewModel> logger)
    {
        _userService = userService;
        _backupService = backupService;
        _settingService = settingService;
        _logger = logger;
        DbPath = _backupService.GetDatabasePath();
    }

    public override async Task LoadAsync()
    {
        try
        {
            _setting = await _settingService.GetAsync();
            CompanyName = _setting.CompanyName;
            CompanyAddress = _setting.Address ?? string.Empty;
            CompanyPhone = _setting.Phone ?? string.Empty;
            BankName = _setting.BankName ?? string.Empty;
            BankAccount = _setting.BankAccount ?? string.Empty;
            InvoiceFooter = _setting.InvoiceFooterNote ?? string.Empty;
            DefaultElectricPrice = _setting.DefaultElectricPrice;
            DefaultWaterPrice = _setting.DefaultWaterPrice;
            LateFeePercent = _setting.LateFeePercent;
        }
        catch (Exception ex) { _logger.LogError(ex, "Error loading settings"); }
    }

    [RelayCommand]
    private async Task SaveSettingsAsync()
    {
        _setting.CompanyName = string.IsNullOrWhiteSpace(CompanyName) ? "NHÀ TRỌ" : CompanyName.Trim();
        _setting.Address = CompanyAddress;
        _setting.Phone = CompanyPhone;
        _setting.BankName = BankName;
        _setting.BankAccount = BankAccount;
        _setting.InvoiceFooterNote = InvoiceFooter;
        _setting.DefaultElectricPrice = DefaultElectricPrice;
        _setting.DefaultWaterPrice = DefaultWaterPrice;
        _setting.LateFeePercent = LateFeePercent;

        SetBusy(true, "Đang lưu cấu hình...");
        try
        {
            if (await _settingService.UpdateAsync(_setting))
                ShowSuccess("Đã lưu cấu hình hệ thống.");
            else ShowError("Lỗi lưu cấu hình.");
        }
        finally { SetBusy(false); }
    }

    [RelayCommand]
    private async Task BackupAsync()
    {
        var dlg = new Microsoft.Win32.SaveFileDialog
        {
            Title = "Chọn nơi lưu bản sao lưu",
            Filter = "Tập tin sao lưu (*.db)|*.db",
            FileName = $"NhaTro_{DateTime.Now:yyyyMMdd_HHmmss}.db"
        };
        if (dlg.ShowDialog() != true) return;

        SetBusy(true, "Đang sao lưu...");
        try
        {
            var path = await _backupService.BackupAsync(dlg.FileName);
            ShowSuccess($"Đã sao lưu thành công vào: {path}");
        }
        catch (Exception ex) { _logger.LogError(ex, "Backup error"); ShowError("Lỗi sao lưu dữ liệu."); }
        finally { SetBusy(false); }
    }

    [RelayCommand]
    private void Restore()
    {
        var dlg = new Microsoft.Win32.OpenFileDialog
        {
            Title = "Chọn file sao lưu để phục hồi",
            Filter = "Tập tin sao lưu (*.db)|*.db"
        };
        if (dlg.ShowDialog() != true) return;

        var confirm = System.Windows.MessageBox.Show(
            "PHỤC HỒI sẽ GHI ĐÈ toàn bộ dữ liệu hiện tại bằng dữ liệu trong file sao lưu.\n\n" +
            "Ứng dụng sẽ khởi động lại sau khi phục hồi. Bạn có chắc chắn?",
            "Xác nhận phục hồi", System.Windows.MessageBoxButton.YesNo, System.Windows.MessageBoxImage.Warning);
        if (confirm != System.Windows.MessageBoxResult.Yes) return;

        try
        {
            _backupService.Restore(dlg.FileName);
            System.Windows.MessageBox.Show("Phục hồi thành công! Ứng dụng sẽ khởi động lại.",
                "Hoàn tất", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);

            // Khởi động lại ứng dụng để nạp dữ liệu mới
            var exe = System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName;
            if (exe != null) System.Diagnostics.Process.Start(exe);
            System.Windows.Application.Current.Shutdown();
        }
        catch (Exception ex) { _logger.LogError(ex, "Restore error"); ShowError("Lỗi phục hồi dữ liệu."); }
    }

    [RelayCommand]
    private async Task ChangePasswordAsync()
    {
        if (string.IsNullOrWhiteSpace(OldPassword) || string.IsNullOrWhiteSpace(NewPassword))
        {
            ShowError("Vui lòng điền đầy đủ thông tin."); return;
        }
        if (NewPassword != ConfirmPassword) { ShowError("Mật khẩu mới không khớp."); return; }
        if (NewPassword.Length < 6) { ShowError("Mật khẩu mới phải có ít nhất 6 ký tự."); return; }

        var userId = LoginViewModel.CurrentUser?.Id ?? 0;
        SetBusy(true, "Đang đổi mật khẩu...");
        try
        {
            if (await _userService.ChangePasswordAsync(userId, OldPassword, NewPassword))
            {
                ShowSuccess("Đổi mật khẩu thành công!");
                OldPassword = NewPassword = ConfirmPassword = string.Empty;
            }
            else ShowError("Mật khẩu cũ không đúng.");
        }
        catch (Exception ex) { _logger.LogError(ex, "Error changing password"); ShowError("Lỗi đổi mật khẩu."); }
        finally { SetBusy(false); }
    }
}
