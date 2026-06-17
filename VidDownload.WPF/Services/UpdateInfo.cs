namespace VidDownload.WPF.Services
{
    public class UpdateInfo
    {
        public string Version { get; set; } = string.Empty;
        public string DownloadUrl { get; set; } = string.Empty;
        public string ReleaseNotes { get; set; } = string.Empty;
        public bool IsPreRelease { get; set; }
        public bool IsUpdateAvailable { get; set; }
    }
}
