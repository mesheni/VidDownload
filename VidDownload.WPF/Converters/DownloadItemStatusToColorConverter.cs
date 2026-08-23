using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using VidDownload.WPF.Services;

namespace VidDownload.WPF.Converters
{
    /// <summary>Цвет полосы-индикатора статуса элемента очереди.</summary>
    public class DownloadItemStatusToColorConverter : IValueConverter
    {
        private static readonly SolidColorBrush QueuedBrush = new(Color.FromRgb(0x9E, 0x9E, 0x9E));
        private static readonly SolidColorBrush DownloadingBrush = new(Color.FromRgb(0x42, 0xA5, 0xF5));
        private static readonly SolidColorBrush PausedBrush = new(Color.FromRgb(0xFB, 0x8C, 0x00));
        private static readonly SolidColorBrush CompletedBrush = new(Color.FromRgb(0x66, 0xBB, 0x6A));
        private static readonly SolidColorBrush FailedBrush = new(Color.FromRgb(0xE5, 0x39, 0x35));
        private static readonly SolidColorBrush CancelledBrush = new(Color.FromRgb(0x75, 0x75, 0x75));

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value switch
            {
                DownloadItemStatus.Downloading => DownloadingBrush,
                DownloadItemStatus.Paused => PausedBrush,
                DownloadItemStatus.Completed => CompletedBrush,
                DownloadItemStatus.Failed => FailedBrush,
                DownloadItemStatus.Cancelled => CancelledBrush,
                _ => QueuedBrush
            };
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
