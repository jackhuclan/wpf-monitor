using IndustrialMonitor.Models;

namespace IndustrialMonitor.Services;

public interface IMockDataService
{
    event Action<DeviceData>? DataArrived;
    void Start();
    void Stop();
    bool TryGetLastData(string deviceId, out DeviceData data);
}
