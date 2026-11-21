using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using IndustrialMonitor.Models;
using IndustrialMonitor.Services;

namespace IndustrialMonitor.ViewModels;

/// <summary>报警中心：按类型筛选 + 分页查询，温度红/压力蓝。</summary>
public partial class AlarmCenterViewModel : ObservableObject, IRefreshable
{
    private const int PageSize = 20;
    private readonly IAlarmLogService _alarmService;
    private readonly IDeviceManagerService _deviceManager;

    public ObservableCollection<AlarmRecord> Records { get; } = new();

    public List<string> TypeOptions { get; } = new() { "全部", "温度", "压力" };

    [ObservableProperty]
    private string _selectedType = "全部";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TotalPages))]
    private int _total;

    [ObservableProperty]
    private int _pageIndex = 1;

    public int TotalPages => Math.Max(1, (int)Math.Ceiling(Total / (double)PageSize));

    public string PageText => $"第 {PageIndex} / {TotalPages} 页 · 共 {Total} 条";

    public AlarmCenterViewModel(IAlarmLogService alarmService, IDeviceManagerService deviceManager)
    {
        _alarmService = alarmService;
        _deviceManager = deviceManager;

        // 新报警落库后，若停留在第一页则实时刷新
        _alarmService.AlarmInserted += _ =>
        {
            if (PageIndex == 1) Load();
            OnPropertyChanged(nameof(PageText));
        };

        Load();
    }

    public void Refresh() => Load();

    partial void OnSelectedTypeChanged(string value) => Load();

    partial void OnTotalChanged(int value) => OnPropertyChanged(nameof(PageText));
    partial void OnPageIndexChanged(int value) => OnPropertyChanged(nameof(PageText));

    private void Load()
    {
        var records = _alarmService.Query(SelectedType, null, null, null, PageIndex, PageSize, out var total);
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
