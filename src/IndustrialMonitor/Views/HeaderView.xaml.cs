using System.Windows.Controls;
using System.Windows.Threading;

namespace IndustrialMonitor.Views;

public partial class HeaderView : UserControl
{
    private readonly DispatcherTimer _clockTimer = new(DispatcherPriority.Background)
    {
        Interval = TimeSpan.FromSeconds(1)
    };

    public HeaderView()
    {
        InitializeComponent();
        UpdateClock();
        _clockTimer.Tick += (_, _) => UpdateClock();
        _clockTimer.Start();
        Loaded += (_, _) => UpdateClock();
    }

    private void UpdateClock() =>
        ClockText.Text = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
}
