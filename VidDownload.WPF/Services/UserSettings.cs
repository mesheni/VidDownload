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
        public string Language { get; set; } = "RU";

        public string ConvertOutputFormat { get; set; } = "MP4";
        public string ConvertVideoCodec { get; set; } = "libx264";
        public string ConvertAudioCodec { get; set; } = "aac";
        public string ConvertHardwareEncoder { get; set; } = string.Empty;
        public int ConvertCrf { get; set; } = 23;
        public int ConvertVideoBitrate { get; set; }
        public int ConvertAudioBitrate { get; set; }
        public string ConvertPreset { get; set; } = "medium";
        public string ConvertOutputDir { get; set; } = string.Empty;
    }
}
