using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using VidDownload.WPF.Services;

namespace VidDownload.WPF.Converters
{
    public class DownloadStatusToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is DownloadStatus status)
            {
                return status switch
                {
                    DownloadStatus.Completed => new SolidColorBrush(Color.FromRgb(0x8B, 0xC3, 0x4A)),
                    DownloadStatus.Failed => new SolidColorBrush(Color.FromRgb(0xFF, 0x52, 0x52)),
                    DownloadStatus.Cancelled => new SolidColorBrush(Color.FromRgb(0xFF, 0xB7, 0x4D)),
                    _ => new SolidColorBrush(Colors.White)
                };
            }
            return new SolidColorBrush(Colors.White);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}