namespace VidDownload.WPF.Control;

/// <summary>
/// Класс Settings определяет свойства для разрешения видео, кодеков и формата.
/// Он содержит два конструктора - один для инициализации свойств и конструктор по умолчанию.
/// Это позволяет создать экземпляр Settings со значениями по умолчанию или пользовательскими значениями.
/// </summary>
public class Settings
{
    public string Resolution { get; set; } = "1080";
    public string VideoCodec { get; set; } = "av01";
    public string AudioCodec { get; set; } = "aac";
    public string Format { get; set; } = "mp4";
    public bool DownloadSubtitles { get; set; }
    public string SubtitleLanguage { get; set; } = "all";
    public bool EmbedSubtitles { get; set; }
    public string SavePath { get; set; } = string.Empty;

    /// <summary>Лимит скорости для yt-dlp (--limit-rate), например "5M" или "500K". Пусто = без лимита.</summary>
    public string RateLimit { get; set; } = string.Empty;

    /// <summary>Точный селектор формата (-f), выбранный в предпросмотре. Пусто = обычная сортировка -S.</summary>
    public string FormatSelector { get; set; } = string.Empty;

    /// <summary>Выбранные элементы плейлиста (--playlist-items), например "1-3,7". Пусто = весь плейлист.</summary>
    public string PlaylistItems { get; set; } = string.Empty;

    /// <summary>Куки из браузера (--cookies-from-browser): chrome/edge/firefox/opera/… Пусто = нет.</summary>
    public string CookiesFromBrowser { get; set; } = string.Empty;

    /// <summary>Путь к файлу cookies.txt (--cookies). Пусто = нет.</summary>
    public string CookiesFile { get; set; } = string.Empty;

    /// <summary>Прокси (--proxy), например "socks5://127.0.0.1:1080". Пусто = без прокси.</summary>
    public string Proxy { get; set; } = string.Empty;

    /// <summary>Количество повторов при ошибках (--retries/--fragment-retries). 0 = по умолчанию yt-dlp.</summary>
    public int Retries { get; set; }

    /// <summary>Пропускать уже скачанное через --download-archive.</summary>
    public bool UseDownloadArchive { get; set; }

    /// <summary>Встраивать обложку в файл (--embed-thumbnail).</summary>
    public bool EmbedThumbnail { get; set; }

    /// <summary>Встраивать метаданные (--embed-metadata).</summary>
    public bool EmbedMetadata { get; set; }

    /// <summary>Качество аудио при извлечении (--audio-quality, 0 — лучшее). Пусто = по умолчанию.</summary>
    public string AudioQuality { get; set; } = string.Empty;

    /// <summary>Конвертировать субтитры в SRT (--convert-subs srt).</summary>
    public bool ConvertSubsToSrt { get; set; }

    /// <summary>Фрагмент по таймкодам (--download-sections), например "*00:01:30-00:05:00". Пусто = целиком.</summary>
    public string DownloadSections { get; set; } = string.Empty;

    public Settings(string resolution, string videoCodec, string audioCodec, string format)
    {
        Resolution = resolution;
        VideoCodec = videoCodec;
        AudioCodec = audioCodec;
        Format = format;
    }

    public Settings() { }

    /// <summary>Независимая копия настроек — каждый элемент очереди работает со своим снимком.</summary>
    public Settings Clone() => new()
    {
        Resolution = Resolution,
        VideoCodec = VideoCodec,
        AudioCodec = AudioCodec,
        Format = Format,
        DownloadSubtitles = DownloadSubtitles,
        SubtitleLanguage = SubtitleLanguage,
        EmbedSubtitles = EmbedSubtitles,
        SavePath = SavePath,
        RateLimit = RateLimit,
        FormatSelector = FormatSelector,
        PlaylistItems = PlaylistItems,
        CookiesFromBrowser = CookiesFromBrowser,
        CookiesFile = CookiesFile,
        Proxy = Proxy,
        Retries = Retries,
        UseDownloadArchive = UseDownloadArchive,
        EmbedThumbnail = EmbedThumbnail,
        EmbedMetadata = EmbedMetadata,
        AudioQuality = AudioQuality,
        ConvertSubsToSrt = ConvertSubsToSrt,
        DownloadSections = DownloadSections
    };
}
