using System.Globalization;
using System.Windows.Data;
using NetPulse.Core.Models;

namespace NetPulse.App.Converters;

public sealed class HealthStateGlyphConverter : IValueConverter
{
    public object Convert(
        object value,
        Type targetType,
        object parameter,
        CultureInfo culture) => value switch
        {
            HealthState.Healthy => "✓",
            HealthState.Degraded => "!",
            HealthState.Offline => "×",
            HealthState.Error => "?",
            _ => "…",
        };

    public object ConvertBack(
        object value,
        Type targetType,
        object parameter,
        CultureInfo culture) => throw new NotSupportedException();
}
