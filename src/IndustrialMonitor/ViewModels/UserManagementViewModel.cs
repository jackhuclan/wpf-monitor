using System.Collections.ObjectModel;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using IndustrialMonitor.Models;
using IndustrialMonitor.Services;

namespace IndustrialMonitor.ViewModels;

/// <summary>用户管理（管理员专属）：新增用户、启用/禁用账户。</summary>
public partial class UserManagementViewModel : ObservableObject, IRefreshable, IUserAware
{
    private readonly IAuthService _authService;

    public ObservableCollection<User> Users { get; } = new();

    public List<string> RoleOptions { get; } = new() { "管理员", "操作员", "观察者" };

    [ObservableProperty]
    private string _newUsername = string.Empty;

    [ObservableProperty]
    private string _newPassword = string.Empty;

    [ObservableProperty]
    private string _newRole = "操作员";

    [ObservableProperty]
    private bool _canManage;

    public UserManagementViewModel(IAuthService authService)
    {
        _authService = authService;
        Load();
    }

    public void SetUserContext(User user) => CanManage = user.IsAdmin;

    public void Refresh() => Load();

    private void Load()
    {
        Users.Clear();
        foreach (var u in _authService.GetUsers()) Users.Add(u);
    }

    [RelayCommand]
    private void AddUser()
    {
        var username = NewUsername.Trim();
        if (string.IsNullOrEmpty(username))
        {
            MessageBox.Show("请输入用户名", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        if (NewPassword.Length < 6)
        {
            MessageBox.Show("密码长度不能少于 6 位", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        if (!_authService.AddUser(username, NewPassword, NewRole))
        {
            MessageBox.Show("用户名已存在", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        NewUsername = string.Empty;
        NewPassword = string.Empty;
        NewRole = "操作员";
        Load();
    }

    [RelayCommand]
    private void ToggleActive(User user)
    {
        _authService.SetUserActive(user.Username, !user.IsActive);
        Load();
    }
}
