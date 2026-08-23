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
        RateLimit = RateLimit
    };
}
