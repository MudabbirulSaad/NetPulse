using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using NetPulse.Core.Models;

namespace NetPulse.App.Converters;

public sealed class HealthStateBrushConverter : IValueConverter
{
    private static readonly Brush Checking = Frozen("#62D6FF");
    private static readonly Brush Healthy = Frozen("#62E6A7");
    private static readonly Brush Degraded = Frozen("#FFC857");
    private static readonly Brush Offline = Frozen("#FF6B6B");
    private static readonly Brush Error = Frozen("#F28CFF");

    public object Convert(
        object value,
        Type targetType,
        object parameter,
        CultureInfo culture) => value switch
        {
            HealthState.Healthy => Healthy,
            HealthState.Degraded => Degraded,
            HealthState.Offline => Offline,
            HealthState.Error => Error,
            _ => Checking,
        };

    public object ConvertBack(
        object value,
        Type targetType,
        object parameter,
        CultureInfo culture) => throw new NotSupportedException();

    private static SolidColorBrush Frozen(string value)
    {
        var brush = (SolidColorBrush)new BrushConverter().ConvertFromString(value)!;
        brush.Freeze();
        return brush;
    }
}
