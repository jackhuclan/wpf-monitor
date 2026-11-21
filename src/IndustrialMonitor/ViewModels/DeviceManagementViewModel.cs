using System.Collections.ObjectModel;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using IndustrialMonitor.Models;
using IndustrialMonitor.Services;

namespace IndustrialMonitor.ViewModels;

/// <summary>设备管理：增删改查 + 绿/红状态圆点。</summary>
public partial class DeviceManagementViewModel : ObservableObject, IRefreshable, IUserAware
{
    private readonly IDeviceManagerService _deviceManager;

    public ObservableCollection<DeviceInfo> Devices { get; } = new();

    [ObservableProperty]
    private bool _canOperate;

    [ObservableProperty]
    private bool _isFormOpen;

    [ObservableProperty]
    private bool _isEditing;

    [ObservableProperty]
    private string _formTitle = "新增设备";

    [ObservableProperty]
    private string _editId = string.Empty;

    [ObservableProperty]
    private string _formName = string.Empty;

    private string? _editingOriginalId;

    public DeviceManagementViewModel(IDeviceManagerService deviceManager)
    {
        _deviceManager = deviceManager;
        _deviceManager.DevicesChanged += () => Application.Current?.Dispatcher.BeginInvoke(Load);
        Load();
    }

    public void SetUserContext(User user) => CanOperate = user.CanOperate;

    public void Refresh() => Load();

    private void Load()
    {
        Devices.Clear();
        foreach (var d in _deviceManager.Devices) Devices.Add(d);
    }

    [RelayCommand]
    private void ShowAdd()
    {
        IsEditing = false;
        IsFormOpen = true;
        FormTitle = "新增设备";
        EditId = $"DEV-{(_deviceManager.Devices.Count + 1):D3}";
        FormName = string.Empty;
        _editingOriginalId = null;
    }

    [RelayCommand]
    private void StartEdit(DeviceInfo device)
    {
        IsEditing = true;
        IsFormOpen = true;
        FormTitle = "编辑设备";
        EditId = device.Id;
        FormName = device.Name;
        _editingOriginalId = device.Id;
    }

    [RelayCommand]
    private void CancelForm() => IsFormOpen = false;

    [RelayCommand]
    private void ConfirmForm()
    {
        var name = FormName.Trim();
        var id = EditId.Trim();
        if (string.IsNullOrEmpty(id) || string.IsNullOrEmpty(name))
        {
            MessageBox.Show("设备编号与名称不能为空", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        try
        {
            if (_editingOriginalId == null)
            {
                _deviceManager.Add(new DeviceInfo { Id = id, Name = name, IsOnline = true });
            }
            else
            {
                var device = _deviceManager.Find(_editingOriginalId);
                if (device == null) return;
                // 编号仅在新增时可改；编辑时保持原编号
                device.Name = name;
                _deviceManager.Update(device);
            }
            IsFormOpen = false;
        }
        catch (InvalidOperationException ex)
        {
            MessageBox.Show(ex.Message, "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    [RelayCommand]
    private void Delete(DeviceInfo device)
    {
        if (MessageBox.Show($"确认删除设备 [{device.Id}] {device.Name} 吗？", "删除确认",
                MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes) return;
        _deviceManager.Remove(device.Id);
    }

    [RelayCommand]
    private void ToggleOnline(DeviceInfo device)
    {
        device.IsOnline = !device.IsOnline;
        _deviceManager.Update(device);
    }
}
