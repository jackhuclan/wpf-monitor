using System.Windows;
using System.Windows.Controls;
using IndustrialMonitor.ViewModels;

namespace IndustrialMonitor.Views;

public partial class SidebarView : UserControl
{
    public SidebarView()
    {
        InitializeComponent();
    }

    private void OnLogoutClick(object sender, RoutedEventArgs e)
    {
        if (DataContext is MainViewModel vm)
            vm.LogoutCommand.Execute(null);
    }
}
