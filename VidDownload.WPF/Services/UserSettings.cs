using System;

namespace VidDownload.WPF.Services
{
    public class UserSettings
    {
        public static string DefaultDownloadPath =>
            Environment.GetFolderPath(Environment.SpecialFolder.MyVideos);

        public string Resolution { get; set; } = string.Empty;
        public string VideoCodec { get; set; } = string.Empty;
        public string AudioCodec { get; set; } = string.Empty;
        public string Format { get; set; } = string.Empty;
        public bool DownloadSubtitles { get; set; }
        public string SubtitleLanguage { get; set; } = string.Empty;
        public bool EmbedSubtitles { get; set; }
        public string SavePath { get; set; } = string.Empty;
    }
}
