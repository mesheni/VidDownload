using System.Windows.Media;
using VidDownload.WPF.Services;

namespace VidDownload.WPF.Themes
{
    /// <summary>
    /// Единая палитра статусов для конвертеров цвета (очередь и история).
    /// Значения синхронизированы с Themes/Shared.xaml и подобраны по дизайн-макетам.
    /// </summary>
    public static class StatusPalette
    {
        public static readonly SolidColorBrush Queued = Frozen(Color.FromRgb(0x8C, 0x93, 0xA0));
        public static readonly SolidColorBrush Downloading = Frozen(Color.FromRgb(0x5B, 0x9D, 0xFF));
        public static readonly SolidColorBrush Paused = Frozen(Color.FromRgb(0xE5, 0xB0, 0x4A));
        public static readonly SolidColorBrush Completed = Frozen(Color.FromRgb(0x4F, 0xC5, 0x8A));
        public static readonly SolidColorBrush Failed = Frozen(Color.FromRgb(0xF0, 0x71, 0x6B));
        public static readonly SolidColorBrush Cancelled = Frozen(Color.FromRgb(0x8C, 0x93, 0xA0));

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
