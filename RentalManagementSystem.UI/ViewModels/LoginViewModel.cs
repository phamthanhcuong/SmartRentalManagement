using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using RentalManagementSystem.Application.Interfaces;
using RentalManagementSystem.Domain.Entities;

namespace RentalManagementSystem.UI.ViewModels;

public partial class LoginViewModel : BaseViewModel
{
    private readonly IUserService _userService;
    private readonly ILogger<LoginViewModel> _logger;

    public static User? CurrentUser { get; private set; }

    [ObservableProperty]
    private string _username = string.Empty;

    [ObservableProperty]
    private string _password = string.Empty;

    [ObservableProperty]
    private bool _rememberMe;

    [ObservableProperty]
    private bool _showPassword;

    public event Action? LoginSucceeded;

    public LoginViewModel(IUserService userService, ILogger<LoginViewModel> logger)
    {
        _userService = userService;
        _logger = logger;

        // Nạp thông tin đăng nhập đã ghi nhớ
        var saved = Services.CredentialStore.Load();
        if (saved != null)
        {
            Username = saved.Value.Username;
            Password = saved.Value.Password;
            RememberMe = true;
        }
    }

    [RelayCommand]
    private async Task LoginAsync()
    {
        if (string.IsNullOrWhiteSpace(Username) || string.IsNullOrWhiteSpace(Password))
        {
            ShowError("Vui lòng nhập tên đăng nhập và mật khẩu.");
            return;
        }

        SetBusy(true, "Đang đăng nhập...");
        ClearMessages();

        try
        {
            var user = await _userService.LoginAsync(Username, Password);
            if (user == null)
            {
                ShowError("Tên đăng nhập hoặc mật khẩu không đúng.");
                return;
            }

            CurrentUser = user;

            // Ghi nhớ hoặc xóa thông tin đăng nhập theo lựa chọn
            if (RememberMe) Services.CredentialStore.Save(Username, Password);
            else Services.CredentialStore.Clear();

            _logger.LogInformation("User {Username} logged in successfully", Username);
            LoginSucceeded?.Invoke();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Login error for {Username}", Username);
            ShowError("Đã xảy ra lỗi. Vui lòng thử lại.");
        }
        finally
        {
            SetBusy(false);
        }
    }

    [RelayCommand]
    private void TogglePassword() => ShowPassword = !ShowPassword;
}
