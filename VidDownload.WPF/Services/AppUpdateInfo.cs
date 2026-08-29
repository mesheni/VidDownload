namespace VidDownload.WPF.Services
{
    public class AppUpdateInfo
    {
        public string Version { get; set; } = string.Empty;
        public string DownloadUrl { get; set; } = string.Empty;

        /// <summary>Ассет Updater.exe из того же релиза (для самообновления обновлятора).</summary>
        public string UpdaterDownloadUrl { get; set; } = string.Empty;
        public string ReleaseNotes { get; set; } = string.Empty;
        public bool IsPreRelease { get; set; }
        public bool IsUpdateAvailable { get; set; }
    }
}
