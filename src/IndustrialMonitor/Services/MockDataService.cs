using System.Collections.Concurrent;
using IndustrialMonitor.Models;

namespace IndustrialMonitor.Services;

/// <summary>高频数据模拟服务：生产者-消费者模式，生产者定时入队，消费者线程推送事件。</summary>
public sealed class MockDataService : IMockDataService, IDisposable
{
    private readonly IDeviceManagerService _deviceManager;
    private readonly IConfigService _configService;

    private BlockingCollection<DeviceData>? _queue;
    private CancellationTokenSource? _cts;
    private Task? _consumerTask;
    private System.Timers.Timer? _producerTimer;
    private readonly Random _random = new();
    private readonly ConcurrentDictionary<string, DeviceData> _lastData = new();

    public event Action<DeviceData>? DataArrived;

    public MockDataService(IDeviceManagerService deviceManager, IConfigService configService)
    {
        _deviceManager = deviceManager;
        _configService = configService;
    }

    public void Start()
    {
        Stop();
        _cts = new CancellationTokenSource();
        _queue = new BlockingCollection<DeviceData>(boundedCapacity: 2048);

        // 消费者：从队列取出数据推送给订阅方
        _consumerTask = Task.Factory.StartNew(
            () => ConsumeLoop(_cts.Token),
            _cts.Token, TaskCreationOptions.LongRunning, TaskScheduler.Default);

        // 生产者：按 PollIntervalMs 定时为在线设备生成随机游走数据
        _producerTimer = new System.Timers.Timer(Math.Max(50, _configService.GetConfig().PollIntervalMs));
        _producerTimer.Elapsed += (_, _) => Produce();
        _producerTimer.AutoReset = true;
        _producerTimer.Start();
    }

    public void Stop()
    {
        _producerTimer?.Stop();
        _producerTimer?.Dispose();
        _producerTimer = null;
        _cts?.Cancel();
        try { _consumerTask?.Wait(500); } catch { /* 忽略取消异常 */ }
        _queue?.CompleteAdding();
        _queue?.Dispose();
        _queue = null;
        _cts?.Dispose();
        _cts = null;
        _consumerTask = null;
    }

    public bool TryGetLastData(string deviceId, out DeviceData data) =>
        _lastData.TryGetValue(deviceId, out data!);

    private void Produce()
    {
        var queue = _queue;
        if (queue == null || queue.IsAddingCompleted) return;

        foreach (var device in _deviceManager.Devices.Where(d => d.IsOnline))
        {
            var data = GenerateRandomWalk(device.Id);
            _lastData[device.Id] = data;
            try { queue.TryAdd(data); } catch { /* 队列已关闭 */ }
        }
    }

    private void ConsumeLoop(CancellationToken token)
    {
        var queue = _queue;
        if (queue == null) return;
        try
        {
            foreach (var data in queue.GetConsumingEnumerable(token))
            {
                DataArrived?.Invoke(data);
            }
        }
        catch (OperationCanceledException)
        {
            // 正常停止
        }
    }

    private double _tempWalk = 78.0;
    private double _pressWalk = 0.95;
    private readonly object _walkLock = new();

    /// <summary>随机游走模拟：围绕基线波动，偶发越限尖峰。</summary>
    private DeviceData GenerateRandomWalk(string deviceId)
    {
        lock (_walkLock)
        {
            _tempWalk += (_random.NextDouble() - 0.48) * 2.4;
            _pressWalk += (_random.NextDouble() - 0.48) * 0.06;

            // 2% 概率产生一个向越限方向的尖峰，便于演示报警
            if (_random.NextDouble() < 0.02) _tempWalk += 4.0 + _random.NextDouble() * 3.0;
            if (_random.NextDouble() < 0.02) _pressWalk += 0.12 + _random.NextDouble() * 0.1;

            // 向基线回归，避免长期漂离
            _tempWalk += (78.0 - _tempWalk) * 0.04;
            _pressWalk += (0.95 - _pressWalk) * 0.04;

            _tempWalk = Math.Clamp(_tempWalk, 55.0, 105.0);
            _pressWalk = Math.Clamp(_pressWalk, 0.4, 1.8);

            return new DeviceData
            {
                DeviceId = deviceId,
                Temperature = Math.Round(_tempWalk, 1),
                Pressure = Math.Round(_pressWalk, 2),
                Timestamp = DateTime.Now
            };
        }
    }

    public void Dispose() => Stop();
}
