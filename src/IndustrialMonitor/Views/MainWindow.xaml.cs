using System.ComponentModel;
using System.Windows;
using IndustrialMonitor.ViewModels;

namespace IndustrialMonitor.Views;

public partial class MainWindow : Window
{
    public MainViewModel ViewModel { get; }

    public MainWindow(MainViewModel viewModel)
    {
        InitializeComponent();
        ViewModel = viewModel;
        DataContext = ViewModel;
        viewModel.LogoutRequested += OnLogoutRequested;
        Closed += (_, _) => viewModel.LogoutRequested -= OnLogoutRequested;
    }

    private void OnLogoutRequested() => App.Logout();

    protected override void OnClosing(CancelEventArgs e)
    {
        base.OnClosing(e);
        if (e.Cancel) return;
        ViewModel.StopMonitoring();
        Application.Current.Shutdown();
    }
}
