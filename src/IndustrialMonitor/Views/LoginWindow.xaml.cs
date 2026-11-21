using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using IndustrialMonitor.Models;
using IndustrialMonitor.ViewModels;

namespace IndustrialMonitor.Views;

public partial class LoginWindow : Window
{
    public LoginViewModel ViewModel { get; }

    public LoginWindow(LoginViewModel viewModel)
    {
        InitializeComponent();
        ViewModel = viewModel;
        DataContext = ViewModel;
        ViewModel.LoginSuccess += OnLoginSuccess;
        Loaded += (_, _) => UsernameBox.Focus();

        UsernameBox.TextChanged += (_, _) => UsernamePlaceholder.Visibility =
            string.IsNullOrEmpty(UsernameBox.Text) ? Visibility.Visible : Visibility.Collapsed;
        PasswordBox.PasswordChanged += (_, _) =>
        {
            PasswordPlaceholder.Visibility = string.IsNullOrEmpty(PasswordBox.Password)
                ? Visibility.Visible : Visibility.Collapsed;
            ErrorText.Text = string.Empty;
        };
    }

    private void OnLoginSuccess(User user) => App.EnterMain(user);

    private void OnLoginClick(object sender, RoutedEventArgs e)
    {
        ViewModel.Username = UsernameBox.Text;
        ViewModel.Password = PasswordBox.Password;
        ViewModel.LoginCommand.Execute(null);
        ErrorText.Text = ViewModel.Error;
    }

    protected override void OnClosing(CancelEventArgs e)
    {
        base.OnClosing(e);
        ViewModel.LoginSuccess -= OnLoginSuccess;
        // 登录窗被直接关闭（无主窗口）时退出程序
        if (Application.Current.Windows.OfType<MainWindow>().All(w => w.IsVisible == false))
            Application.Current.Shutdown();
    }

    private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e) => DragMove();
}
