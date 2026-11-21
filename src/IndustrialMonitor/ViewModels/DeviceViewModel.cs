using System.Collections.ObjectModel;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using IndustrialMonitor.Models;
using IndustrialMonitor.Services;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using LiveChartsCore.SkiaSharpView.Painting.Effects;
using SkiaSharp;

namespace IndustrialMonitor.ViewModels;

/// <summary>单设备监控 ViewModel：滑动窗口图表数据源 + 防抖报警引擎。</summary>
public partial class DeviceViewModel : ObservableObject, IDisposable
{
    private static readonly SKColor TempLineColor = new(0x2E, 0x86, 0xF0);      // 品牌蓝
    private static readonly SKColor PressLineColor = new(0x22, 0xB0, 0x8C);     // 青绿
    private static readonly SKColor TempAlarmColor = new(0xF0, 0x47, 0x3C);     // 红
    private static readonly SKColor PressAlarmColor = new(0xF5, 0xA6, 0x23);    // 橙
    private static readonly SKColor AxisTextColor = new(0x7F, 0x8F, 0xA4);
    private static readonly SKColor SeparatorColor = new(0xE8, 0xEA, 0xF0);

    private readonly IConfigService _configService;
    private readonly IAlarmLogService _alarmService;
    private readonly DispatcherTimer _flushTimer;

    private readonly Queue<DateTime> _pendingTime = new();
    private readonly Queue<double> _pendingTemp = new();
    private readonly Queue<double> _pendingPress = new();
    private readonly List<DateTime> _timeBuffer = new();
    private DateTime? _alarmStartTime;
    private bool _disposed;

    public DeviceInfo Device { get; }

    public ObservableCollection<double> TempValues { get; } = new();
    public ObservableCollection<double> PressValues { get; } = new();

    /// <summary>该设备最近的报警列表（图表下方展示）。</summary>
    public ObservableCollection<AlarmRecord> RecentAlarms { get; } = new();

    public ISeries[] Series { get; private set; } = Array.Empty<ISeries>();
    public Axis[] XAxes { get; private set; } = Array.Empty<Axis>();
    public Axis[] YAxes { get; private set; } = Array.Empty<Axis>();

    /// <summary>阈值警戒虚线段（挂在图表 Sections 上）。</summary>
    public RectangularSection[] Sections { get; private set; } = Array.Empty<RectangularSection>();

    [ObservableProperty]
    private double _lastTemperature;

    [ObservableProperty]
    private double _lastPressure;

    [ObservableProperty]
    private bool _isAlarm;

    [ObservableProperty]
    private string _windowInfoText = string.Empty;

    public DeviceViewModel(DeviceInfo device, IConfigService configService, IAlarmLogService alarmService)
    {
        Device = device;
        _configService = configService;
        _alarmService = alarmService;

        BuildChart();

        _flushTimer = new DispatcherTimer(DispatcherPriority.Background)
        {
            Interval = TimeSpan.FromMilliseconds(Math.Max(100, _configService.GetConfig().RefreshIntervalMs))
        };
        _flushTimer.Tick += (_, _) => FlushChart();
        _flushTimer.Start();

        _configService.ConfigChanged += OnConfigChanged;
        UpdateWindowInfo(_configService.GetConfig());
    }

    /// <summary>数据点到来（UI 线程）：进入待刷新缓冲，同时执行报警引擎。</summary>
    public void OnData(DeviceData data)
    {
        if (_disposed) return;
        _pendingTime.Enqueue(data.Timestamp);
        _pendingTemp.Enqueue(data.Temperature);
        _pendingPress.Enqueue(data.Pressure);
        LastTemperature = data.Temperature;
        LastPressure = data.Pressure;

        CheckAlarm(data);
    }

    // ==================== 报警引擎（防抖） ====================
    private void CheckAlarm(DeviceData data)
    {
        var config = _configService.GetConfig();
        var isTempAlarm = data.Temperature > config.TemperatureThreshold;
        var isPressAlarm = data.Pressure > config.PressureThreshold;
        var overThreshold = isTempAlarm || isPressAlarm;

        // 报警灯：越限期间呼吸闪烁
        IsAlarm = overThreshold;

        if (overThreshold)
        {
            if (!_alarmStartTime.HasValue)
            {
                _alarmStartTime = DateTime.Now;
            }
            else if ((DateTime.Now - _alarmStartTime.Value).TotalSeconds >= config.AlarmDebounceSeconds)
            {
                TriggerAlarm(data, isTempAlarm, isPressAlarm, config);
                _alarmStartTime = null;
            }
        }
        else
        {
            _alarmStartTime = null;
        }
    }

    private void TriggerAlarm(DeviceData data, bool isTempAlarm, bool isPressAlarm, MonitorConfig config)
    {
        var type = isTempAlarm ? "温度" : "压力";
        double value = isTempAlarm ? data.Temperature : data.Pressure;
        double threshold = isTempAlarm ? config.TemperatureThreshold : config.PressureThreshold;
        var unit = isTempAlarm ? "℃" : "MPa";

        var record = new AlarmRecord
        {
            DeviceId = Device.Id,
            Type = type,
            Value = value,
            Threshold = threshold,
            Message = $"{type} {value:F1}{unit} > {threshold:F1}{unit}",
            Time = DateTime.Now
        };
        _alarmService.Insert(record);

        RecentAlarms.Insert(0, record);
        while (RecentAlarms.Count > 50) RecentAlarms.RemoveAt(RecentAlarms.Count - 1);
    }

    // ==================== 图表 ====================
    private void FlushChart()
    {
        if (_disposed) return;
        var window = Math.Max(10, _configService.GetConfig().WindowSize);

        while (_pendingTime.Count > 0)
        {
            _timeBuffer.Add(_pendingTime.Dequeue());
            TempValues.Add(_pendingTemp.Dequeue());
            PressValues.Add(_pendingPress.Dequeue());
        }
        while (TempValues.Count > window)
        {
            TempValues.RemoveAt(0);
            PressValues.RemoveAt(0);
            _timeBuffer.RemoveAt(0);
        }
    }

    private string LabelTime(double value)
    {
        var idx = (int)value;
        if (idx < 0 || idx >= _timeBuffer.Count) return string.Empty;
        return _timeBuffer[idx].ToString("HH:mm:ss");
    }

    private void OnConfigChanged(MonitorConfig config)
    {
        if (_disposed) return;
        // 热重载：阈值虚线与刷新间隔立即生效
        BuildAxes(config);
        OnPropertyChanged(nameof(YAxes));
        _flushTimer.Interval = TimeSpan.FromMilliseconds(Math.Max(100, config.RefreshIntervalMs));
        UpdateWindowInfo(config);
    }

    private void UpdateWindowInfo(MonitorConfig config) =>
        WindowInfoText = $"窗口 {TempValues.Count}/{config.WindowSize} 点 · 刷新 {config.RefreshIntervalMs}ms · 防抖 {config.AlarmDebounceSeconds}s";

    private void BuildChart()
    {
        BuildSeries();
        BuildAxes(_configService.GetConfig());
    }

    private void BuildSeries()
    {
        Series = new ISeries[]
        {
            new LineSeries<double>
            {
                Values = TempValues,
                Name = "温度",
                ScalesYAt = 0,
                GeometrySize = 0,
                LineSmoothness = 0.6,
                Stroke = new SolidColorPaint(TempLineColor, 2),
                Fill = new SolidColorPaint(TempLineColor.WithAlpha(30))
            },
            new LineSeries<double>
            {
                Values = PressValues,
                Name = "压力",
                ScalesYAt = 1,
                GeometrySize = 0,
                LineSmoothness = 0.6,
                Stroke = new SolidColorPaint(PressLineColor, 2),
                Fill = new SolidColorPaint(PressLineColor.WithAlpha(30))
            }
        };
        OnPropertyChanged(nameof(Series));
    }

    private void BuildAxes(MonitorConfig config)
    {
        var labelsPaint = new SolidColorPaint(AxisTextColor);
        var separatorPaint = new SolidColorPaint(SeparatorColor);

        XAxes = new Axis[]
        {
            new Axis
            {
                LabelsPaint = labelsPaint,
                SeparatorsPaint = separatorPaint,
                Labeler = LabelTime,
                MinStep = 10
            }
        };

        YAxes = new Axis[]
        {
            new Axis
            {
                Name = "温度(℃)",
                NamePaint = new SolidColorPaint(TempLineColor),
                LabelsPaint = labelsPaint,
                SeparatorsPaint = separatorPaint
            },
            new Axis
            {
                Name = "压力(MPa)",
                NamePaint = new SolidColorPaint(PressLineColor),
                LabelsPaint = labelsPaint,
                SeparatorsPaint = null
            }
        };

        // 阈值警戒线：虚线绘制在各自坐标轴上
        Sections = new RectangularSection[]
        {
            new RectangularSection
            {
                Yi = config.TemperatureThreshold,
                Xi = 0,
                Xj = 1,
                ScalesYAt = 0,
                Stroke = new SolidColorPaint(TempAlarmColor, 1)
                {
                    PathEffect = new DashEffect(new float[] { 6f, 4f })
                }
            },
            new RectangularSection
            {
                Yi = config.PressureThreshold,
                Xi = 0,
                Xj = 1,
                ScalesYAt = 1,
                Stroke = new SolidColorPaint(PressAlarmColor, 1)
                {
                    PathEffect = new DashEffect(new float[] { 6f, 4f })
                }
            }
        };
        OnPropertyChanged(nameof(XAxes));
        OnPropertyChanged(nameof(Sections));
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _flushTimer.Stop();
        _configService.ConfigChanged -= OnConfigChanged;
    }
}
