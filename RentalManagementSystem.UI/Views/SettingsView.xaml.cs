using System.Windows.Controls;
using RentalManagementSystem.UI.ViewModels;

namespace RentalManagementSystem.UI.Views;

public partial class SettingsView : UserControl
{
    public SettingsView() => InitializeComponent();

    private void OldPwdBox_Changed(object sender, System.Windows.RoutedEventArgs e)
    {
        if (DataContext is SettingsViewModel vm) vm.OldPassword = OldPwdBox.Password;
    }

    private void NewPwdBox_Changed(object sender, System.Windows.RoutedEventArgs e)
    {
        if (DataContext is SettingsViewModel vm) vm.NewPassword = NewPwdBox.Password;
    }

    private void ConfirmPwdBox_Changed(object sender, System.Windows.RoutedEventArgs e)
    {
        if (DataContext is SettingsViewModel vm) vm.ConfirmPassword = ConfirmPwdBox.Password;
    }
}
