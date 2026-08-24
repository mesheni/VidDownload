using System.Windows.Media;
using VidDownload.WPF.Services;

namespace VidDownload.WPF.Themes
{
    /// <summary>
    /// Единая палитра статусов для конвертеров цвета (очередь и история).
    /// Значения синхронизированы с Themes/Colors.xaml.
    /// </summary>
    public static class StatusPalette
    {
        public static readonly SolidColorBrush Queued = Frozen(Color.FromRgb(0x9E, 0x9E, 0x9E));
        public static readonly SolidColorBrush Downloading = Frozen(Color.FromRgb(0x42, 0xA5, 0xF5));
        public static readonly SolidColorBrush Paused = Frozen(Color.FromRgb(0xFB, 0x8C, 0x00));
        public static readonly SolidColorBrush Completed = Frozen(Color.FromRgb(0x66, 0xBB, 0x6A));
        public static readonly SolidColorBrush Failed = Frozen(Color.FromRgb(0xE5, 0x39, 0x35));
        public static readonly SolidColorBrush Cancelled = Frozen(Color.FromRgb(0x75, 0x75, 0x75));

        public static SolidColorBrush For(DownloadItemStatus status) => status switch
        {
            DownloadItemStatus.Downloading => Downloading,
            DownloadItemStatus.Paused => Paused,
            DownloadItemStatus.Completed => Completed,
            DownloadItemStatus.Failed => Failed,
            DownloadItemStatus.Cancelled => Cancelled,
            _ => Queued
        };

        public static SolidColorBrush For(DownloadStatus status) => status switch
        {
            DownloadStatus.Completed => Completed,
            DownloadStatus.Failed => Failed,
            DownloadStatus.Cancelled => Cancelled,
            _ => Queued
        };

        private static SolidColorBrush Frozen(Color color)
        {
            var brush = new SolidColorBrush(color);
            brush.Freeze();
            return brush;
        }
    }
}
