using System.Windows;
using System.Windows.Input;
using RentalManagementSystem.UI.ViewModels;

namespace RentalManagementSystem.UI.Views;

public partial class LoginWindow : Window
{
    private readonly LoginViewModel _viewModel;

    private bool _syncing;

    public LoginWindow(LoginViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = viewModel;
        viewModel.LoginSucceeded += OnLoginSucceeded;
        viewModel.PropertyChanged += ViewModel_PropertyChanged;

        // Hiển thị mật khẩu đã ghi nhớ vào ô PasswordBox
        if (!string.IsNullOrEmpty(viewModel.Password))
            PasswordBox.Password = viewModel.Password;

        // Allow window dragging
        MouseLeftButtonDown += (s, e) => DragMove();
    }

    // Đồng bộ PasswordBox với mật khẩu khi bật/tắt "hiện mật khẩu"
    private void ViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(LoginViewModel.ShowPassword) && !_viewModel.ShowPassword)
        {
            _syncing = true;
            PasswordBox.Password = _viewModel.Password;
            _syncing = false;
        }
    }

    private void OnLoginSucceeded()
    {
        var mainWindow = App.GetService<MainWindow>();
        mainWindow.Show();
        Close();
    }

    private void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
    {
        if (!_syncing && DataContext is LoginViewModel vm)
            vm.Password = PasswordBox.Password;
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e) => System.Windows.Application.Current.Shutdown();
}
