using CommunityToolkit.Mvvm.ComponentModel;

namespace RentalManagementSystem.UI.ViewModels;

public abstract partial class BaseViewModel : ObservableObject
{
    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private string _busyMessage = "Đang tải...";

    [ObservableProperty]
    private string? _errorMessage;

    [ObservableProperty]
    private string? _successMessage;

    protected void SetBusy(bool busy, string message = "Đang xử lý...")
    {
        IsBusy = busy;
        BusyMessage = message;
    }

    protected void ShowError(string message)
    {
        ErrorMessage = message;
        SuccessMessage = null;
    }

    protected void ShowSuccess(string message)
    {
        SuccessMessage = message;
        ErrorMessage = null;
    }

    protected void ClearMessages()
    {
        ErrorMessage = null;
        SuccessMessage = null;
    }

    public virtual Task LoadAsync() => Task.CompletedTask;

    /// <summary>
    /// Hộp thoại xác nhận xóa rõ ràng, kèm tên đối tượng.
    /// Ví dụ ConfirmDelete("phòng P101") -> "Bạn sắp XÓA phòng P101 — KHÔNG thể hoàn lại...".
    /// Mặc định nút an toàn (Không).
    /// </summary>
    protected static bool ConfirmDelete(string what)
    {
        var msg = $"Bạn sắp XÓA {what}.\n\n"
                + "⚠ Hành động này KHÔNG THỂ hoàn lại.\n\n"
                + "Bạn có chắc chắn muốn xóa không?";
        return System.Windows.MessageBox.Show(
            msg, "XÁC NHẬN XÓA",
            System.Windows.MessageBoxButton.YesNo,
            System.Windows.MessageBoxImage.Warning,
            System.Windows.MessageBoxResult.No) == System.Windows.MessageBoxResult.Yes;
    }

    /// <summary>Yêu cầu điều hướng về Trang Chủ (dùng nút "Trang Chủ" trên các trang).</summary>
    [CommunityToolkit.Mvvm.Input.RelayCommand]
    protected void GoHome() => MainViewModel.NavigateRequested?.Invoke("TrangChu");
}
