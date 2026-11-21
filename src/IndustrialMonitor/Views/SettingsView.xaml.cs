using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using IndustrialMonitor.ViewModels;

namespace IndustrialMonitor.Views;

public partial class SettingsView : UserControl
{
    public SettingsView()
    {
        InitializeComponent();
    }

    private SettingsViewModel? VM => DataContext as SettingsViewModel;

    private void OnChangePasswordClick(object sender, RoutedEventArgs e)
    {
        if (VM == null) return;
        var ok = VM.TryChangePassword(OldPasswordBox.Password, NewPasswordBox.Password,
            ConfirmPasswordBox.Password, out var error);
        PasswordMessage.Text = ok ? "密码修改成功" : error;
        PasswordMessage.Foreground = ok
            ? new SolidColorBrush(Color.FromRgb(0x2E, 0xA0, 0x43))
            : new SolidColorBrush(Color.FromRgb(0xE5, 0x48, 0x4D));
        if (ok)
        {
            OldPasswordBox.Clear();
            NewPasswordBox.Clear();
            ConfirmPasswordBox.Clear();
        }
    }
}
