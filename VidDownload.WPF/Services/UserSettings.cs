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

        /// <summary>Лимит скорости yt-dlp (--limit-rate), например "5M". Пусто = без лимита.</summary>
        public string RateLimit { get; set; } = string.Empty;

        /// <summary>Максимум одновременных загрузок очереди (1–3).</summary>
        public int MaxConcurrentDownloads { get; set; } = 1;

        /// <summary>Сворачивать приложение в трей при закрытии окна вместо выхода.</summary>
        public bool MinimizeToTray { get; set; }

        /// <summary>Следить за буфером обмена и предлагать добавить ссылки в очередь.</summary>
        public bool ClipboardMonitorEnabled { get; set; }

        /// <summary>Тема интерфейса: "Auto" — следовать системе, либо явные "Light"/"Dark".</summary>
        public string Appearance { get; set; } = "Dark";

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
