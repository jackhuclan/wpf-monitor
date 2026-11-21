namespace IndustrialMonitor.Models;

/// <summary>设备信息，JSON 持久化到 devices.json。</summary>
public class DeviceInfo
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public bool IsOnline { get; set; } = true;
    public string Remark { get; set; } = string.Empty;
}
