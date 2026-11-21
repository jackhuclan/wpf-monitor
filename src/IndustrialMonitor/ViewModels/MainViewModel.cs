using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using IndustrialMonitor.Models;
using IndustrialMonitor.Services;
using IndustrialMonitor.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace IndustrialMonitor.ViewModels;

/// <summary>主外壳 ViewModel：管理导航切换、菜单角色过滤与当前用户。</summary>
public partial class MainViewModel : ObservableObject
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IMockDataService _mockDataService;

    [ObservableProperty]
    private object? _currentView;

    [ObservableProperty]
    private string _currentTitle = "首页总览";

    [ObservableProperty]
    private User? _currentUser;

    [ObservableProperty]
    private ObservableCollection<NavMenuItem> _menuItems = new();

    public MainViewModel(IServiceProvider serviceProvider, IMockDataService mockDataService)
    {
        _serviceProvider = serviceProvider;
        _mockDataService = mockDataService;
    }

    /// <summary>登录成功后由主窗口调用。</summary>
    public void SetUser(User user)
    {
        CurrentUser = user;
        BuildMenu();
        Navigate(typeof(DashboardViewModel));
        _mockDataService.Start();
    }

    /// <summary>退出登录前停止数据模拟。</summary>
    public void StopMonitoring() => _mockDataService.Stop();

    private void BuildMenu()
    {
        var role = CurrentUser!.Role;
        var allRoles = new[] { "管理员", "操作员", "观察者" };
        var menus = new List<NavMenuItem>
        {
            new() { Title = "首页总览", Glyph = "\uE80F", TargetViewModelType = typeof(DashboardViewModel), AllowedRoles = allRoles },
            new() { Title = "实时监控", Glyph = "\uE9D9", TargetViewModelType = typeof(MonitoringViewModel), AllowedRoles = allRoles },
            new() { Title = "报警中心", Glyph = "\uE7BA", TargetViewModelType = typeof(AlarmCenterViewModel), AllowedRoles = allRoles },
            new() { Title = "设备管理", Glyph = "\uE772", TargetViewModelType = typeof(DeviceManagementViewModel), AllowedRoles = new[] { "管理员", "操作员" } },
            new() { Title = "历史记录", Glyph = "\uE81C", TargetViewModelType = typeof(HistoryViewModel), AllowedRoles = allRoles },
            new() { Title = "系统设置", Glyph = "\uE713", TargetViewModelType = typeof(SettingsViewModel), AllowedRoles = new[] { "管理员", "操作员" } },
            new() { Title = "用户管理", Glyph = "\uE77B", TargetViewModelType = typeof(UserManagementViewModel), AllowedRoles = new[] { "管理员" } },
        };
        MenuItems = new ObservableCollection<NavMenuItem>(menus.Where(m => m.AllowedRoles.Contains(role)));
    }

    [RelayCommand]
    private void NavigateTo(NavMenuItem item) => Navigate(item.TargetViewModelType);

    public void Navigate(Type viewModelType)
    {
        foreach (var m in MenuItems)
            m.IsActive = m.TargetViewModelType == viewModelType;
        OnPropertyChanged(nameof(MenuItems));

        CurrentView = _serviceProvider.GetRequiredService(viewModelType);
        CurrentTitle = MenuItems.FirstOrDefault(m => m.TargetViewModelType == viewModelType)?.Title
                       ?? viewModelType.Name.Replace("ViewModel", string.Empty);

        if (CurrentUser != null && CurrentView is IUserAware aware)
            aware.SetUserContext(CurrentUser);
        if (CurrentView is IRefreshable refreshable)
            refreshable.Refresh();
    }

    [RelayCommand]
    private void Logout()
    {
        StopMonitoring();
        LogoutRequested?.Invoke();
    }

    /// <summary>退出登录请求（由 App 处理，切回登录窗）。</summary>
    public event Action? LogoutRequested;
}
