namespace IndustrialMonitor.Models;

/// <summary>监控配置强类型模型，对应 appsettings.json，支持热重载。</summary>
public class MonitorConfig
{
    public double TemperatureThreshold { get; set; } = 85.0;
    public double PressureThreshold { get; set; } = 1.2;
    public int RefreshIntervalMs { get; set; } = 400;
    public int WindowSize { get; set; } = 60;
    public int AlarmDebounceSeconds { get; set; } = 3;
    public int PollIntervalMs { get; set; } = 200;
}
