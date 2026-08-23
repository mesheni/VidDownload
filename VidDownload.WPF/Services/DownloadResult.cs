using System;

namespace VidDownload.WPF.Services
{
    /// <summary>Итог загрузки: путь к файлу и вычисленное из него название видео.</summary>
    public class DownloadResult
    {
        public string FilePath { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
    }
}
