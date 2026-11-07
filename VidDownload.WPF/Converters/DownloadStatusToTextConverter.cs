using System;
using System.Globalization;
using System.Windows.Data;
using VidDownload.WPF.Resources;
using VidDownload.WPF.Services;

namespace VidDownload.WPF.Converters
{
    /// <summary>Локализованное название статуса записи истории загрузок.</summary>
    public class DownloadStatusToTextConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return LocalizedStrings.Instance[GetResourceKey(value)];
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }

        /// <summary>Ключ ресурса для статуса истории (общий для конвертера и фильтра поиска).</summary>
        public static string GetResourceKey(object value)
        {
            return value switch
            {
                DownloadStatus.Completed => "StatusCompleted",
                DownloadStatus.Failed => "StatusFailed",
                DownloadStatus.Cancelled => "StatusCancelled",
                _ => "StatusCompleted"
            };
        }
    }
}
