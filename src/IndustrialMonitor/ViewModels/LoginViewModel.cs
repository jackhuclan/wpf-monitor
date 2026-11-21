using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using IndustrialMonitor.Models;
using IndustrialMonitor.Services;

namespace IndustrialMonitor.ViewModels;

public partial class LoginViewModel : ObservableObject
{
    private readonly IAuthService _authService;

    [ObservableProperty]
    private string _username = string.Empty;

    /// <summary>由登录窗口代码后台从 PasswordBox 注入（PasswordBox 不可绑定）。</summary>
    public string Password { get; set; } = string.Empty;

    [ObservableProperty]
    private string _error = string.Empty;

    /// <summary>登录成功回调。</summary>
    public event Action<User>? LoginSuccess;

    public LoginViewModel(IAuthService authService)
    {
        _authService = authService;
    }

    [RelayCommand]
    private void Login()
    {
        Error = string.Empty;
        if (string.IsNullOrWhiteSpace(Username))
        {
            Error = "请输入用户名";
            return;
        }
        if (string.IsNullOrEmpty(Password))
        {
            Error = "请输入密码";
            return;
        }

        var user = _authService.Login(Username.Trim(), Password);
        if (user == null)
        {
            Error = "用户名或密码错误，或该账户已被禁用";
            return;
        }
        LoginSuccess?.Invoke(user);
    }
}
