using System;
using System.Globalization;
using System.Windows.Data;
using VidDownload.WPF.Services;
using VidDownload.WPF.Themes;

namespace VidDownload.WPF.Converters
{
    /// <summary>Цвет полосы-индикатора статуса элемента очереди.</summary>
    public class DownloadItemStatusToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value is DownloadItemStatus status
                ? StatusPalette.For(status)
                : StatusPalette.Queued;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
