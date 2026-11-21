namespace IndustrialMonitor.Models;

/// <summary>设备数据点：温度、压力、时间戳。</summary>
public class DeviceData
{
    public string DeviceId { get; set; } = string.Empty;
    public double Temperature { get; set; }
    public double Pressure { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.Now;
}
