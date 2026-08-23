using System;
using System.Globalization;
using System.Windows.Data;
using VidDownload.WPF.Resources;
using VidDownload.WPF.Services;

namespace VidDownload.WPF.Converters
{
    /// <summary>Локализованное название статуса элемента очереди.</summary>
    public class DownloadItemStatusToTextConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string key = value switch
            {
                DownloadItemStatus.Queued => "StatusQueued",
                DownloadItemStatus.Downloading => "StatusDownloading",
                DownloadItemStatus.Paused => "StatusPaused",
                DownloadItemStatus.Completed => "StatusCompleted",
                DownloadItemStatus.Failed => "StatusFailed",
                DownloadItemStatus.Cancelled => "StatusCancelled",
                _ => "StatusQueued"
            };
            return LocalizedStrings.Instance[key];
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
