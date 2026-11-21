using System.IO;
using System.Text.Json;
using IndustrialMonitor.Models;

namespace IndustrialMonitor.Services;

/// <summary>设备生命周期管理：JSON 文件持久化，关机不丢。</summary>
public sealed class DeviceManagerService : IDeviceManagerService
{
    private readonly string _path;
    private readonly List<DeviceInfo> _devices = new();
    private readonly object _lock = new();
    private static readonly JsonSerializerOptions ReadOptions = new() { PropertyNameCaseInsensitive = true };

    public event Action? DevicesChanged;

    public DeviceManagerService()
    {
        _path = Path.Combine(AppContext.BaseDirectory, "devices.json");
        Load();
    }

    public List<DeviceInfo> Devices
    {
        get { lock (_lock) return _devices.Select(d => Clone(d)).ToList(); }
    }

    private static DeviceInfo Clone(DeviceInfo d) =>
        new() { Id = d.Id, Name = d.Name, IsOnline = d.IsOnline, Remark = d.Remark };

    public DeviceInfo? Find(string id)
    {
        lock (_lock) return _devices.FirstOrDefault(d => d.Id == id) is { } d ? Clone(d) : null;
    }

    public void Add(DeviceInfo device)
    {
        lock (_lock)
        {
            if (_devices.Any(d => d.Id == device.Id)) throw new InvalidOperationException("设备编号已存在");
            _devices.Add(Clone(device));
            Save();
        }
        DevicesChanged?.Invoke();
    }

    public void Update(DeviceInfo device)
    {
        lock (_lock)
        {
            var idx = _devices.FindIndex(d => d.Id == device.Id);
            if (idx < 0) return;
            _devices[idx] = Clone(device);
            Save();
        }
        DevicesChanged?.Invoke();
    }

    public void Remove(string id)
    {
        lock (_lock)
        {
            if (_devices.RemoveAll(d => d.Id == id) <= 0) return;
            Save();
        }
        DevicesChanged?.Invoke();
    }

    /// <summary>仅触发变更通知（如状态圆点更新），不落盘。</summary>
    public void RaiseChanged() => DevicesChanged?.Invoke();

    private void Load()
    {
        lock (_lock)
        {
            try
            {
                if (File.Exists(_path))
                {
                    var json = File.ReadAllText(_path);
                    var list = JsonSerializer.Deserialize<List<DeviceInfo>>(json, ReadOptions);
                    if (list != null)
                    {
                        _devices.Clear();
                        _devices.AddRange(list);
                        return;
                    }
                }
            }
            catch
            {
                // 文件损坏时重建默认设备
            }

            // 首次启动写入默认设备
            _devices.Clear();
            _devices.AddRange(new List<DeviceInfo>
            {
                new() { Id = "DEV-001", Name = "空压机组", IsOnline = true },
                new() { Id = "DEV-002", Name = "冷却水泵", IsOnline = true },
                new() { Id = "DEV-003", Name = "反应釜A区", IsOnline = true },
                new() { Id = "DEV-004", Name = "注塑机B线", IsOnline = true },
            });
            Save();
        }
    }

    private void Save()
    {
        var json = JsonSerializer.Serialize(_devices, new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });
        File.WriteAllText(_path, json);
    }
}
