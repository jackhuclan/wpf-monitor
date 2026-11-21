using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using IndustrialMonitor.Services;

namespace IndustrialMonitor.ViewModels;

/// <summary>首页仪表盘：统计卡片 + 在线率 + 系统信息，2 秒定时刷新。</summary>
public partial class DashboardViewModel : ObservableObject, IRefreshable
{
    private static readonly DateTime StartupTime = DateTime.Now;

    private readonly IDeviceManagerService _deviceManager;
    private readonly IAlarmLogService _alarmService;
    private readonly IMockDataService _mockDataService;
    private readonly IConfigService _configService;
    private readonly DispatcherTimer _timer;

    [ObservableProperty]
    private int _totalDevices;

    [ObservableProperty]
    private int _onlineDevices;

    [ObservableProperty]
    private int _todayAlarms;

    [ObservableProperty]
    private double _avgTemperature;

    [ObservableProperty]
    private double _onlineRate;

    [ObservableProperty]
    private string _runDurationText = "0分钟";

    [ObservableProperty]
    private string _refreshIntervalText = "400ms";

    [ObservableProperty]
    private string _dbInfoText = string.Empty;

    public DashboardViewModel(IDeviceManagerService deviceManager, IAlarmLogService alarmService,
        IMockDataService mockDataService, IConfigService configService)
    {
        _deviceManager = deviceManager;
        _alarmService = alarmService;
        _mockDataService = mockDataService;
        _configService = configService;

        _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2) };
        _timer.Tick += (_, _) => UpdateStats();
        _timer.Start();

        UpdateStats();
    }

    public void Refresh() => UpdateStats();

    private void UpdateStats()
    {
        var devices = _deviceManager.Devices;
        TotalDevices = devices.Count;
        OnlineDevices = devices.Count(d => d.IsOnline);
        TodayAlarms = _alarmService.CountToday();
        OnlineRate = TotalDevices > 0 ? Math.Round(OnlineDevices * 100.0 / TotalDevices, 1) : 0;

        var temps = new List<double>();
        foreach (var d in devices)
        {
            if (_mockDataService.TryGetLastData(d.Id, out var data))
                temps.Add(data.Temperature);
        }
        AvgTemperature = temps.Count > 0 ? Math.Round(temps.Average(), 1) : 0;

        var span = DateTime.Now - StartupTime;
        RunDurationText = span.TotalHours >= 1
            ? $"{(int)span.TotalHours}小时{span.Minutes}分"
            : $"{(int)span.TotalMinutes}分{span.Seconds}秒";

        var cfg = _configService.GetConfig();
        RefreshIntervalText = $"{cfg.RefreshIntervalMs}ms";
        DbInfoText = $"SQLite(users.db) + JSON(devices.json)  |  报警防抖 {cfg.AlarmDebounceSeconds}s  |  窗口 {cfg.WindowSize} 点";
    }
}
