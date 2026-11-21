using IndustrialMonitor.Models;

namespace IndustrialMonitor.Services;

public interface IDeviceManagerService
{
    List<DeviceInfo> Devices { get; }
    event Action? DevicesChanged;
    DeviceInfo? Find(string id);
    void Add(DeviceInfo device);
    void Update(DeviceInfo device);
    void Remove(string id);
    void RaiseChanged();
}
