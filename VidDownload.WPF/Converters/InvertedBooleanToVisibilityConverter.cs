using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace VidDownload.WPF.Converters
{
    /// <summary>false → Visible, true → Collapsed (например, пустое состояние списка).</summary>
    public class InvertedBooleanToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value is false ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value is Visibility.Collapsed;
        }
    }
}
