using System.IO;
using System.Text;
using System.Text.Json;
using System.Windows;
using System.Windows.Threading;
using IndustrialMonitor.Models;

namespace IndustrialMonitor.Services;

/// <summary>配置读取服务：自带 appsettings.json 文件变更监听，支持热重载。</summary>
public sealed class ConfigService : IConfigService, IDisposable
{
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true,
        NumberHandling = System.Text.Json.Serialization.JsonNumberHandling.AllowReadingFromString
    };

    private readonly string _path;
    private FileSystemWatcher? _watcher;
    private MonitorConfig _current;

    public event Action<MonitorConfig>? ConfigChanged;

    public ConfigService()
    {
        _path = Path.Combine(AppContext.BaseDirectory, "appsettings.json");
        _current = Load();

        var dir = Path.GetDirectoryName(_path)!;
        if (Directory.Exists(dir))
        {
            _watcher = new FileSystemWatcher(dir, Path.GetFileName(_path))
            {
                NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size
            };
            _watcher.Changed += OnFileChanged;
            _watcher.EnableRaisingEvents = true;
        }
    }

    public MonitorConfig GetConfig() => _current;

    public void Save(MonitorConfig config)
    {
        var json = JsonSerializer.Serialize(config, new JsonSerializerOptions
        {
            WriteIndented = true
        });
        File.WriteAllText(_path, json, Encoding.UTF8);
        Apply(config);
    }

    private void Apply(MonitorConfig config)
    {
        _current = config;
        // 通知走 UI 线程，订阅方无需自行调度
        var dispatcher = Application.Current?.Dispatcher ?? Dispatcher.CurrentDispatcher;
        dispatcher.BeginInvoke(() => ConfigChanged?.Invoke(config));
    }

    private MonitorConfig Load()
    {
        try
        {
            if (File.Exists(_path))
            {
                var json = File.ReadAllText(_path);
                var cfg = JsonSerializer.Deserialize<MonitorConfig>(json, JsonOpts);
                if (cfg != null) return cfg;
            }
        }
        catch
        {
            // 读取失败时退回默认值，保证程序可用
        }
        return new MonitorConfig();
    }

    private DateTime _lastEvent = DateTime.MinValue;
    private void OnFileChanged(object sender, FileSystemEventArgs e)
    {
        // VS / 编辑器保存会连续触发多次，做简单去抖
        if ((DateTime.Now - _lastEvent).TotalMilliseconds < 400) return;
        _lastEvent = DateTime.Now;
        try { Thread.Sleep(200); Apply(Load()); } catch { /* 文件被占用时忽略，等待下次事件 */ }
    }

    public void Dispose()
    {
        _watcher?.Dispose();
        _watcher = null;
    }
}
