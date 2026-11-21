using System.IO;
using System.Windows;
using IndustrialMonitor.Models;
using IndustrialMonitor.Services;
using IndustrialMonitor.ViewModels;
using IndustrialMonitor.Views;
using Microsoft.Extensions.DependencyInjection;

namespace IndustrialMonitor;

public partial class App : Application
{
    public static IServiceProvider Services { get; private set; } = default!;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // 全局异常兜底：写入 crash.log 便于现场排查
        DispatcherUnhandledException += (_, args) =>
        {
            LogCrash(args.Exception);
            MessageBox.Show("程序发生未处理异常：" + args.Exception.Message + "\n详细信息见安装目录 crash.log",
                "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            args.Handled = true;
        };
        AppDomain.CurrentDomain.UnhandledException += (_, args) =>
        {
            if (args.ExceptionObject is Exception ex) LogCrash(ex);
        };

        Services = ConfigureServices();

        if (e.Args.Contains("--smoke"))
        {
            SmokeTest.Run();
            return;
        }

        ShowLogin();
    }

    private static void LogCrash(Exception ex)
    {
        try
        {
            File.WriteAllText(Path.Combine(AppContext.BaseDirectory, "crash.log"),
                $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}]\n{ex}\n");
        }
        catch
        {
            // 忽略日志写入失败
        }
    }

    private static void ShowLogin()
    {
        var login = Services.GetRequiredService<LoginWindow>();
        login.Show();
    }

    /// <summary>登录成功后打开主窗口并关闭登录窗口。</summary>
    public static void EnterMain(User User)
    {
        var main = Services.GetRequiredService<MainWindow>();
        main.ViewModel.SetUser(User);
        main.Show();

        // 关闭所有登录窗口
        foreach (Window w in Current.Windows.OfType<Window>().Where(w => w is LoginWindow).ToList())
        {
            w.Close();
        }
    }

    /// <summary>退出登录：回到登录界面。</summary>
    public static void Logout()
    {
        foreach (Window w in Current.Windows.OfType<Window>().Where(w => w is MainWindow).ToList())
        {
            var main = (MainWindow)w;
            main.ViewModel.StopMonitoring();
            w.Close();
        }
        ShowLogin();
    }

    private static IServiceProvider ConfigureServices()
    {
        var services = new ServiceCollection();

        // Services
        services.AddSingleton<IConfigService, ConfigService>();
        services.AddSingleton<IAuthService, AuthService>();
        services.AddSingleton<IAlarmLogService, AlarmLogService>();
        services.AddSingleton<IDeviceManagerService, DeviceManagerService>();
        services.AddSingleton<IMockDataService, MockDataService>();

        // ViewModels
        services.AddSingleton<LoginViewModel>();
        services.AddSingleton<MainViewModel>();
        services.AddSingleton<DashboardViewModel>();
        services.AddSingleton<MonitoringViewModel>();
        services.AddSingleton<AlarmCenterViewModel>();
        services.AddSingleton<DeviceManagementViewModel>();
        services.AddSingleton<HistoryViewModel>();
        services.AddSingleton<SettingsViewModel>();
        services.AddSingleton<UserManagementViewModel>();

        // Windows
        services.AddSingleton<LoginWindow>();
        services.AddSingleton<MainWindow>();

        return services.BuildServiceProvider();
    }
}
