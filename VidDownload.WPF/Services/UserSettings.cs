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

        /// <summary>Действие после завершения очереди: "" — ничего, Shutdown/Sleep/Hibernate.</summary>
        public string PostQueueAction { get; set; } = string.Empty;

        /// <summary>Куки из браузера для yt-dlp (--cookies-from-browser): chrome/edge/firefox/opera. Пусто = нет.</summary>
        public string CookiesFromBrowser { get; set; } = string.Empty;

        /// <summary>Путь к cookies.txt (--cookies). Пусто = нет.</summary>
        public string CookiesFile { get; set; } = string.Empty;

        /// <summary>Прокси (--proxy). Пусто = без прокси.</summary>
        public string Proxy { get; set; } = string.Empty;

        /// <summary>Повторы при ошибках yt-dlp (--retries). 0 = по умолчанию.</summary>
        public int Retries { get; set; } = 3;

        /// <summary>Пропускать уже скачанные видео (--download-archive).</summary>
        public bool UseDownloadArchive { get; set; }

        /// <summary>Встраивать обложку в файл (--embed-thumbnail).</summary>
        public bool EmbedThumbnail { get; set; }

        /// <summary>Встраивать метаданные (--embed-metadata).</summary>
        public bool EmbedMetadata { get; set; }

        /// <summary>Качество аудио по умолчанию при извлечении (--audio-quality). Пусто = по умолчанию.</summary>
        public string AudioQuality { get; set; } = string.Empty;

        /// <summary>Конвертировать субтитры в SRT по умолчанию.</summary>
        public bool ConvertSubsToSrt { get; set; }

        /// <summary>Сворачивать приложение в трей при закрытии окна вместо выхода.</summary>
        public bool MinimizeToTray { get; set; }

        /// <summary>Следить за буфером обмена и предлагать добавить ссылки в очередь.</summary>
        public bool ClipboardMonitorEnabled { get; set; }

        /// <summary>Тема интерфейса: "Auto" — следовать системе, либо явные "Light"/"Dark".</summary>
        public string Appearance { get; set; } = "Dark";

        public string ConvertOutputFormat { get; set; } = "MP4";

        /// <summary>Конвертер в режиме «только аудио» при последнем использовании.</summary>
        public bool ConvertAudioOnlyMode { get; set; }
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
