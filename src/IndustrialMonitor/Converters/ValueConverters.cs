using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace IndustrialMonitor.Converters;

/// <summary>报警类型 → 颜色（温度 → 红，压力 → 蓝）。</summary>
public class AlarmTypeColorConverter : IValueConverter
{
    private static readonly Brush TempBrush = new SolidColorBrush(Color.FromRgb(0xF0, 0x47, 0x3C));
    private static readonly Brush PressBrush = new SolidColorBrush(Color.FromRgb(0x3D, 0x7E, 0xF0));
    private static readonly Brush OtherBrush = new SolidColorBrush(Color.FromRgb(0x9A, 0xA3, 0xB5));

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => (value as string) switch
        {
            "温度" => TempBrush,
            "压力" => PressBrush,
            _ => OtherBrush
        };

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

/// <summary>设备报警状态 → 圆点颜色（true → 红，false → 绿）。</summary>
public class AlarmDotConverter : IValueConverter
{
    private static readonly Brush AlarmBrush = new SolidColorBrush(Color.FromRgb(0xE5, 0x48, 0x4D));
    private static readonly Brush NormalBrush = new SolidColorBrush(Color.FromRgb(0x2E, 0xA0, 0x43));

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value is true ? AlarmBrush : NormalBrush;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

/// <summary>在线状态 → 圆点颜色（true → 绿，false → 灰）。</summary>
public class OnlineDotConverter : IValueConverter
{
    private static readonly Brush OnlineBrush = new SolidColorBrush(Color.FromRgb(0x2E, 0xA0, 0x43));
    private static readonly Brush OfflineBrush = new SolidColorBrush(Color.FromRgb(0x9A, 0xA3, 0xB5));

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value is true ? OnlineBrush : OfflineBrush;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

/// <summary>布尔值 → 可见性（true → Visible，false → Hidden）。</summary>
public class BoolToHiddenVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value is true ? Visibility.Visible : Visibility.Hidden;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => value is Visibility.Visible;
}

/// <summary>布尔值取反。</summary>
public class InverseBoolConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value is not true;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => value is false;
}

/// <summary>账户启用状态 → 按钮文字（启用/禁用）。</summary>
public class ActiveButtonTextConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value is true ? "禁用" : "启用";

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

/// <summary>布尔值 → 可见性（true → Collapsed，false → Visible）。</summary>
public class InverseBoolToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value is true ? Visibility.Collapsed : Visibility.Visible;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => value is Visibility.Visible;
}

/// <summary>账户启用状态 → 状态文字（正常/已禁用）。</summary>
public class AccountStatusTextConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value is true ? "正常" : "已禁用";

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

/// <summary>在线状态 → 文字（在线/离线）。</summary>
public class OnlineTextConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value is true ? "在线" : "离线";

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
