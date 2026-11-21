using System.Collections.ObjectModel;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using IndustrialMonitor.Models;
using IndustrialMonitor.Services;

namespace IndustrialMonitor.ViewModels;

/// <summary>历史记录：日期范围 + 设备筛选 + 分页。</summary>
public partial class HistoryViewModel : ObservableObject, IRefreshable
{
    private const int PageSize = 20;
    private readonly IAlarmLogService _alarmService;
    private readonly IDeviceManagerService _deviceManager;

    public ObservableCollection<AlarmRecord> Records { get; } = new();

    public List<string> DeviceOptions { get; private set; } = new();

    [ObservableProperty]
    private string _selectedDevice = "全部设备";

    [ObservableProperty]
    private DateTime _startDate = DateTime.Today.AddDays(-1);

    [ObservableProperty]
    private DateTime _endDate = DateTime.Today;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TotalPages))]
    private int _total;

    [ObservableProperty]
    private int _pageIndex = 1;

    public int TotalPages => Math.Max(1, (int)Math.Ceiling(Total / (double)PageSize));

    public string PageText => $"第 {PageIndex} / {TotalPages} 页 · 共 {Total} 条";

    public HistoryViewModel(IAlarmLogService alarmService, IDeviceManagerService deviceManager)
    {
        _alarmService = alarmService;
        _deviceManager = deviceManager;
        LoadDevices();
        Load();
    }

    public void Refresh()
    {
        LoadDevices();
        Load();
    }

    private void LoadDevices()
    {
        var options = new List<string> { "全部设备" };
        options.AddRange(_deviceManager.Devices.Select(d => $"{d.Id} {d.Name}"));
        DeviceOptions = options;
        OnPropertyChanged(nameof(DeviceOptions));
        if (!DeviceOptions.Contains(SelectedDevice)) SelectedDevice = "全部设备";
    }

    partial void OnPageIndexChanged(int value) => OnPropertyChanged(nameof(PageText));
    partial void OnTotalChanged(int value) => OnPropertyChanged(nameof(PageText));

    [RelayCommand]
    private void Search()
    {
        PageIndex = 1;
        Load();
    }

    private void Load()
    {
        var deviceKey = SelectedDevice.Split(' ')[0];
        var records = _alarmService.Query(null, SelectedDevice == "全部设备" ? null : deviceKey,
            StartDate, EndDate, PageIndex, PageSize, out var total);
        Total = total;
        Records.Clear();
        foreach (var r in records) Records.Add(r);
    }

    [RelayCommand]
    private void PrevPage()
    {
        if (PageIndex > 1) { PageIndex--; Load(); }
    }

    [RelayCommand]
    private void NextPage()
    {
        if (PageIndex < TotalPages) { PageIndex++; Load(); }
    }
}
