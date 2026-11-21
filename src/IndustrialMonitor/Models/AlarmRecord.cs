namespace IndustrialMonitor.Models;

/// <summary>报警记录，映射 SQLite AlarmLogs 表。</summary>
public class AlarmRecord
{
    public long Id { get; set; }
    public string DeviceId { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;      // 温度 / 压力
    public double Value { get; set; }
    public double Threshold { get; set; }
    public string Message { get; set; } = string.Empty;   // 如：温度 87.5℃ > 85℃
    public DateTime Time { get; set; } = DateTime.Now;
}
