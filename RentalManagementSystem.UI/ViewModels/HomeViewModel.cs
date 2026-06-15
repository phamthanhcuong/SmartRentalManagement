using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace RentalManagementSystem.UI.ViewModels;

/// <summary>
/// Trang chủ đơn giản dành cho người dùng lớn tuổi: các ô lớn dẫn thẳng tới việc cần làm,
/// giảm tối đa số thao tác (1 chạm là tới nơi).
/// </summary>
public partial class HomeViewModel : BaseViewModel
{
    [ObservableProperty] private string _greeting = string.Empty;
    [ObservableProperty] private string _today = string.Empty;

    public HomeViewModel()
    {
        var name = LoginViewModel.CurrentUser?.FullName ?? "bạn";
        var h = DateTime.Now.Hour;
        var chao = h < 11 ? "Chào buổi sáng" : h < 14 ? "Chào buổi trưa" : h < 18 ? "Chào buổi chiều" : "Chào buổi tối";
        Greeting = $"{chao}, {name}!";
        Today = "Hôm nay là " + DateTime.Now.ToString("dddd, 'ngày' dd 'tháng' MM 'năm' yyyy",
            new System.Globalization.CultureInfo("vi-VN"));
    }

    public override Task LoadAsync() => Task.CompletedTask;

    // Một chạm điều hướng tới màn hình tương ứng
    [RelayCommand]
    private void Go(string menu) => MainViewModel.NavigateRequested?.Invoke(menu);
}
