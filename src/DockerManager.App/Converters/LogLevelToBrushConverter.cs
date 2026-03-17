using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using DockerManager.Core.Models;

namespace DockerManager.App.Converters;

public class LogLevelToBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is LogLevel level)
        {
            return level switch
            {
                LogLevel.Error => Brushes.Red,
                LogLevel.Warning => Brushes.Orange,
                LogLevel.Success => Brushes.Green,
                _ => Brushes.White
            };
        }
        return Brushes.White;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
