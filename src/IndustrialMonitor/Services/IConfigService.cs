using IndustrialMonitor.Models;

namespace IndustrialMonitor.Services;

public interface IConfigService
{
    MonitorConfig GetConfig();
    event Action<MonitorConfig>? ConfigChanged;
    void Save(MonitorConfig config);
}
