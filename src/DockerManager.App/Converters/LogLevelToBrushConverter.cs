using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using DockerManager.Core.Models;

namespace DockerManager.App.Converters;

public class LogLevelToBrushConverter : IValueConverter
{
    private static readonly SolidColorBrush ErrorBrush = new(Color.FromRgb(0xBA, 0x1A, 0x1A));
    private static readonly SolidColorBrush WarningBrush = new(Color.FromRgb(0xC8, 0x70, 0x00));
    private static readonly SolidColorBrush SuccessBrush = new(Color.FromRgb(0x10, 0x7C, 0x10));
    private static readonly SolidColorBrush DefaultBrush = new(Color.FromRgb(0x19, 0x1C, 0x1E));

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is LogLevel level)
        {
            return level switch
            {
                LogLevel.Error => ErrorBrush,
                LogLevel.Warning => WarningBrush,
                LogLevel.Success => SuccessBrush,
                _ => DefaultBrush
            };
        }
        return DefaultBrush;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
