using System.Globalization;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using IndustrialMonitor.Models;
using IndustrialMonitor.Services;

namespace IndustrialMonitor.ViewModels;

/// <summary>系统设置：参数表单（保存即生效）+ 修改密码。</summary>
public partial class SettingsViewModel : ObservableObject, IRefreshable, IUserAware
{
    private readonly IConfigService _configService;
    private readonly IAuthService _authService;

    [ObservableProperty]
    private string _temperatureThresholdText = string.Empty;

    [ObservableProperty]
    private string _pressureThresholdText = string.Empty;

    [ObservableProperty]
    private string _refreshIntervalText = string.Empty;

    [ObservableProperty]
    private string _windowSizeText = string.Empty;

    [ObservableProperty]
    private string _debounceSecondsText = string.Empty;

    [ObservableProperty]
    private string _saveMessage = string.Empty;

    [ObservableProperty]
    private bool _canOperate;

    private string _currentUsername = string.Empty;

    public SettingsViewModel(IConfigService configService, IAuthService authService)
    {
        _configService = configService;
        _authService = authService;
        LoadFromConfig();
        _configService.ConfigChanged += _ => Application.Current?.Dispatcher.BeginInvoke(LoadFromConfig);
    }

    public void SetUserContext(User user)
    {
        CanOperate = user.CanOperate;
        _currentUsername = user.Username;
    }

    public void Refresh() => LoadFromConfig();

    private void LoadFromConfig()
    {
        var cfg = _configService.GetConfig();
        TemperatureThresholdText = cfg.TemperatureThreshold.ToString(CultureInfo.InvariantCulture);
        PressureThresholdText = cfg.PressureThreshold.ToString(CultureInfo.InvariantCulture);
        RefreshIntervalText = cfg.RefreshIntervalMs.ToString();
        WindowSizeText = cfg.WindowSize.ToString();
        DebounceSecondsText = cfg.AlarmDebounceSeconds.ToString();
    }

    [RelayCommand]
    private void SaveConfig()
    {
        SaveMessage = string.Empty;

        if (!TryParse(TemperatureThresholdText, out var tempThr, 0, 200, "温度阈值") |
            !TryParse(PressureThresholdText, out var pressThr, 0, 10, "压力阈值") |
            !TryParse(RefreshIntervalText, out var refreshMs, 100, 10000, "刷新间隔") |
            !TryParse(WindowSizeText, out var window, 10, 500, "滑动窗口") |
            !TryParse(DebounceSecondsText, out var debounce, 1, 60, "报警防抖"))
        {
            return;
        }

        _configService.Save(new MonitorConfig
        {
            TemperatureThreshold = tempThr,
            PressureThreshold = pressThr,
            RefreshIntervalMs = (int)refreshMs,
            WindowSize = (int)window,
            AlarmDebounceSeconds = (int)debounce,
            PollIntervalMs = Math.Max(50, _configService.GetConfig().PollIntervalMs)
        });
        SaveMessage = "保存成功，参数已实时生效（无需重启）";
    }

    private static bool TryParse(string text, out double value, double min, double max, string fieldName)
    {
        if (!double.TryParse(text.Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out value)
            || value < min || value > max)
        {
            MessageBox.Show($"{fieldName} 输入无效（需为 {min} ~ {max} 之间的数值）", "校验失败",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return false;
        }
        return true;
    }

    /// <summary>修改密码：旧密码校验通过后更新。</summary>
    public bool TryChangePassword(string oldPassword, string newPassword, string confirmPassword, out string error)
    {
        error = string.Empty;
        if (string.IsNullOrEmpty(oldPassword)) { error = "请输入旧密码"; return false; }
        if (newPassword.Length < 6) { error = "新密码长度不能少于 6 位"; return false; }
        if (newPassword != confirmPassword) { error = "两次输入的新密码不一致"; return false; }
        if (!_authService.ChangePassword(_currentUsername, oldPassword, newPassword))
        {
            error = "旧密码不正确";
            return false;
        }
        return true;
    }
}
