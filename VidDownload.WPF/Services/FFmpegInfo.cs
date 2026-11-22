namespace VidDownload.WPF.Services
{
    public class FFmpegInfo
    {
        public string LocalVersion { get; set; } = string.Empty;
        public string LatestVersion { get; set; } = string.Empty;
        public string DownloadUrl { get; set; } = string.Empty;

        /// <summary>Отпечаток сборки (тег, имя и дата ассета) для сравнения с установленной.</summary>
        public string Stamp { get; set; } = string.Empty;
        public bool IsUpdateAvailable { get; set; }
    }
}
