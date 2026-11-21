using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using IndustrialMonitor.Models;
using IndustrialMonitor.Services;

namespace IndustrialMonitor.ViewModels;

/// <summary>实时监控页：管理多个设备 ViewModel。</summary>
public partial class MonitoringViewModel : ObservableObject, IRefreshable
{
    private readonly IDeviceManagerService _deviceManager;
    private readonly IMockDataService _mockDataService;
    private readonly IAlarmLogService _alarmService;
    private readonly IConfigService _configService;

    public ObservableCollection<DeviceViewModel> Devices { get; } = new();

    [ObservableProperty]
    private DeviceViewModel? _selectedDevice;

    public MonitoringViewModel(IDeviceManagerService deviceManager, IMockDataService mockDataService,
        IAlarmLogService alarmService, IConfigService configService)
    {
        _deviceManager = deviceManager;
        _mockDataService = mockDataService;
        _alarmService = alarmService;
        _configService = configService;

        _mockDataService.DataArrived += OnDataArrived;
        _deviceManager.DevicesChanged += () => Application.Current?.Dispatcher.BeginInvoke(RebuildDevices);

        RebuildDevices();
    }

    public void Refresh() => RebuildDevices();

    private void RebuildDevices()
    {
        var devices = _deviceManager.Devices;
        foreach (var vm in Devices.Where(v => devices.All(d => d.Id != v.Device.Id)).ToList())
        {
            vm.Dispose();
            Devices.Remove(vm);
        }
        foreach (var d in devices)
        {
            if (Devices.Any(v => v.Device.Id == d.Id)) continue;
            Devices.Add(new DeviceViewModel(d, _configService, _alarmService));
        }
        if (SelectedDevice == null || Devices.All(v => v.Device.Id != SelectedDevice.Device.Id))
            SelectedDevice = Devices.FirstOrDefault();
    }

    private void OnDataArrived(DeviceData data)
    {
        Application.Current?.Dispatcher.BeginInvoke(() =>
        {
            var vm = Devices.FirstOrDefault(v => v.Device.Id == data.DeviceId);
            vm?.OnData(data);
        });
    }
}
