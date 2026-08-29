using System.Windows.Media;
using VidDownload.WPF.Services;

namespace VidDownload.WPF.Themes
{
    /// <summary>
    /// Единая палитра статусов для конвертеров цвета (очередь и история).
    /// Значения синхронизированы с Themes/Shared.xaml и подобраны универсально
    /// для светлой и тёмной темы.
    /// </summary>
    public static class StatusPalette
    {
        public static readonly SolidColorBrush Queued = Frozen(Color.FromRgb(0x8A, 0x8A, 0x8A));
        public static readonly SolidColorBrush Downloading = Frozen(Color.FromRgb(0x35, 0x74, 0xF0));
        public static readonly SolidColorBrush Paused = Frozen(Color.FromRgb(0xDC, 0x68, 0x03));
        public static readonly SolidColorBrush Completed = Frozen(Color.FromRgb(0x34, 0xA8, 0x53));
        public static readonly SolidColorBrush Failed = Frozen(Color.FromRgb(0xD3, 0x2F, 0x2F));
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
