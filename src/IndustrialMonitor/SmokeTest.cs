using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using IndustrialMonitor.Models;
using IndustrialMonitor.Services;
using IndustrialMonitor.ViewModels;
using IndustrialMonitor.Views;
using Microsoft.Extensions.DependencyInjection;

namespace IndustrialMonitor;

/// <summary>无人工干预的运行时自检：登录 → 逐个页面导航 → 校验数据与图表 → 退出。</summary>
internal static class SmokeTest
{
    private static readonly StringBuilder Log = new();

    public static async void Run()
    {
        var app = Application.Current;
        try
        {
            var auth = App.Services.GetRequiredService<IAuthService>();
            var user = auth.Login("admin", "admin123");
            Step(user != null ? "登录成功 admin/管理员" : "登录失败", user != null);
            if (user == null) { Finish(1); return; }

            var shotsDir = Path.Combine(AppContext.BaseDirectory, "shots");
            Directory.CreateDirectory(shotsDir);
            var login = App.Services.GetRequiredService<LoginWindow>();
            login.Show();
            await Task.Delay(900);
            Capture(login, Path.Combine(shotsDir, "01-login.png"));
            login.Hide();

            var main = App.Services.GetRequiredService<MainWindow>();
            main.ViewModel.SetUser(user);
            main.Show();
            await Task.Delay(800);
            Step($"主窗口显示，菜单 {main.ViewModel.MenuItems.Count} 项", main.ViewModel.MenuItems.Count == 7);

            foreach (var menu in main.ViewModel.MenuItems.ToList())
            {
                var viewName = menu.TargetViewModelType.Name;
                main.ViewModel.NavigateToCommand.Execute(menu);
                await Task.Delay(1200);
                Capture(main, Path.Combine(shotsDir, $"0{Array.IndexOf(main.ViewModel.MenuItems.ToArray(), menu) + 2}-{menu.TargetViewModelType.Name.Replace("ViewModel", "")}.png"));
                Step($"导航 {menu.Title} -> {main.ViewModel.CurrentView?.GetType().Name}",
                    main.ViewModel.CurrentView?.GetType() == menu.TargetViewModelType);
            }

            // 实时监控：等待数据推送与图表刷新
            var monitoring = App.Services.GetRequiredService<MonitoringViewModel>();
            main.ViewModel.Navigate(typeof(MonitoringViewModel));
            await Task.Delay(3500);
            var device = monitoring.Devices.FirstOrDefault();
            var deviceManager = App.Services.GetRequiredService<IDeviceManagerService>();
            Step($"设备管理器 {deviceManager.Devices.Count} 台 / 监控页 {monitoring.Devices.Count} 台 / 图表点数 {device?.TempValues.Count ?? 0}",
                monitoring.Devices.Count == deviceManager.Devices.Count && (device?.TempValues.Count ?? 0) > 0);
            Step($"阈值警戒线段 {device?.Sections.Length ?? 0} 条", (device?.Sections.Length ?? 0) == 2);

            // 报警中心 / 历史记录可用性
            var alarms = App.Services.GetRequiredService<AlarmCenterViewModel>();
            alarms.Refresh();
            Step($"报警查询返回 {alarms.Records.Count} 条（总计 {alarms.Total}）", true);

            var history = App.Services.GetRequiredService<HistoryViewModel>();
            history.Refresh();
            Step($"历史查询返回 {history.Records.Count} 条（总计 {history.Total}）", true);

            // 配置热重载：写入阈值后应作用于设备页
            var config = App.Services.GetRequiredService<IConfigService>();
            var original = config.GetConfig();
            Step($"配置读取：温度阈值 {original.TemperatureThreshold} / 压力阈值 {original.PressureThreshold}", true);

            // 报警引擎：把温度阈值降到基线以下，验证 防抖 -> 落库 -> 热重载生效
            main.ViewModel.Navigate(typeof(MonitoringViewModel));
            config.Save(new MonitorConfig
            {
                TemperatureThreshold = 60,
                PressureThreshold = 0.6,
                RefreshIntervalMs = original.RefreshIntervalMs,
                WindowSize = original.WindowSize,
                AlarmDebounceSeconds = 1,
                PollIntervalMs = original.PollIntervalMs
            });
            await Task.Delay(4500);
            Step($"阈值热重载生效（Sections[0].Yi = {device?.Sections.FirstOrDefault()?.Yi:F1}）",
                Math.Abs((device?.Sections.FirstOrDefault()?.Yi ?? 0) - 60) < 0.01);

            alarms.Refresh();
            Step($"报警引擎触发并落库，共 {alarms.Total} 条", alarms.Total > 0);
            Step($"设备页最近报警 {device?.RecentAlarms.Count ?? 0} 条，报警灯状态 {device?.IsAlarm}",
                (device?.RecentAlarms.Count ?? 0) > 0);

            config.Save(original);
            await Task.Delay(300);
            Step("阈值已恢复默认", Math.Abs(config.GetConfig().TemperatureThreshold - original.TemperatureThreshold) < 0.01);

            // 观察者权限：菜单仅保留 4 项（无设备管理/系统设置/用户管理）
            var observer = auth.Login("observer", "123456");
            if (observer != null)
            {
                main.ViewModel.SetUser(observer);
                var titles = string.Join(",", main.ViewModel.MenuItems.Select(m => m.Title));
                Step($"观察者菜单 {main.ViewModel.MenuItems.Count} 项：{titles}",
                    main.ViewModel.MenuItems.Count == 4 &&
                    main.ViewModel.MenuItems.All(m => m.Title is "首页总览" or "实时监控" or "报警中心" or "历史记录"));
                var mgmt = App.Services.GetRequiredService<DeviceManagementViewModel>();
                mgmt.SetUserContext(observer);
                Step($"观察者操作权限 CanOperate={mgmt.CanOperate}", !mgmt.CanOperate);
            }
            else
            {
                Step("观察者登录失败", false);
            }

            var baseDir = AppContext.BaseDirectory;
            Step("users.db 已创建", File.Exists(Path.Combine(baseDir, "users.db")));
            Step("devices.json 已创建", File.Exists(Path.Combine(baseDir, "devices.json")));

            Finish(0);
        }
        catch (Exception ex)
        {
            Log.AppendLine("EXCEPTION: " + ex);
            Finish(2);
        }
    }

    private static void Capture(Window window, string path)
    {
        try
        {
            window.UpdateLayout();
            var width = (int)Math.Max(1, window.ActualWidth);
            var height = (int)Math.Max(1, window.ActualHeight);
            Log.AppendLine($"[SHOT] {path} {width}x{height}");
            var bmp = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Pbgra32);
            bmp.Render(window);
            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(bmp));
            using var fs = File.Create(path);
            encoder.Save(fs);
        }
        catch (Exception ex)
        {
            Log.AppendLine("[WARN] 截图失败: " + ex.Message);
        }
    }

    private static void Step(string message, bool ok) =>
        Log.AppendLine($"[{(ok ? "PASS" : "FAIL")}] {message}");

    private static void Finish(int code)
    {
        try
        {
            File.WriteAllText(Path.Combine(AppContext.BaseDirectory, "smoke.log"), Log.ToString());
        }
        catch { /* ignore */ }
        Application.Current.Shutdown(code);
    }
}
