using System;
using System.Collections.Generic;
using System.Linq;

namespace VidDownload.WPF.Control
{
    /// <summary>Метаданные видео/плейлиста из `yt-dlp -J` (упрощённый набор полей).</summary>
    public class VideoInfo
    {
        public string Id { get; set; } = string.Empty;

        public string Title { get; set; } = string.Empty;

        public string Uploader { get; set; } = string.Empty;

        /// <summary>Длительность в секундах.</summary>
        public double? Duration { get; set; }

        public string Thumbnail { get; set; } = string.Empty;

        public string WebpageUrl { get; set; } = string.Empty;

        public List<FormatInfo> Formats { get; set; } = new();

        public List<PlaylistEntryInfo> Entries { get; set; } = new();

        /// <summary>Результат является плейлистом (список Entries непуст).</summary>
        public bool IsPlaylistResult => Entries.Count > 0;
    }

    /// <summary>Один формат из списка formats yt-dlp.</summary>
    public class FormatInfo
    {
        public string FormatId { get; set; } = string.Empty;

        public string Ext { get; set; } = string.Empty;

        public string Resolution { get; set; } = string.Empty;

        public string VCodec { get; set; } = string.Empty;

        public string ACodec { get; set; } = string.Empty;

        public double? Fps { get; set; }

        public long? Filesize { get; set; }

        public string FormatNote { get; set; } = string.Empty;

        public bool IsAudioOnly => string.IsNullOrEmpty(VCodec) || VCodec == "none";

        public bool IsVideoOnly => string.IsNullOrEmpty(ACodec) || ACodec == "none";

        /// <summary>Компактная подпись для списка: «1080p · mp4 · av01 · ~120 МБ».</summary>
        public string DisplayText
        {
            get
            {
                var parts = new List<string>(4);

                string quality = !string.IsNullOrEmpty(FormatNote)
                    ? FormatNote
                    : Resolution;
                if (IsAudioOnly)
                {
                    parts.Add("audio");
                    if (!string.IsNullOrEmpty(Ext))
                        parts.Add(Ext);
                }
                else
                {
                    if (!string.IsNullOrEmpty(quality))
                        parts.Add(quality);
                    if (!string.IsNullOrEmpty(Ext))
                        parts.Add(Ext);
                    var codec = ShortCodec(VCodec);
                    if (!string.IsNullOrEmpty(codec))
                        parts.Add(codec);
                }

                if (!IsAudioOnly && Filesize is > 0)
                    parts.Add(FormatSize(Filesize.Value));

                return string.Join(" · ", parts);
            }
        }

        /// <summary>Короткое имя кодека: «av01.0.08M.08» → «av01», «vp9.2 (Profile 2)» → «vp9.2», «vp09.00.10.08» → «vp9».</summary>
        public static string ShortCodec(string codec)
        {
            if (string.IsNullOrEmpty(codec))
                return string.Empty;
            if (codec.StartsWith("vp9.", StringComparison.Ordinal) && codec.Contains("2"))
                return "vp9.2";
            if (codec.StartsWith("vp09.", StringComparison.Ordinal))
                return "vp9";
            return codec.Split('.')[0];
        }

        public static string FormatSize(long bytes) => bytes >= 1L << 30
            ? $"{bytes / (double)(1L << 30):0.#} GiB"
            : $"{bytes / (double)(1L << 20):0.#} MiB";
    }

    /// <summary>Элемент плейлиста из `yt-dlp -J --flat-playlist`.</summary>
    public class PlaylistEntryInfo
    {
        public string Id { get; set; } = string.Empty;

        public string Title { get; set; } = string.Empty;

        public double? Duration { get; set; }

        public string Url { get; set; } = string.Empty;

        /// <summary>Номер в плейлисте (1-based).</summary>
        public int Index { get; set; }

        public string DurationText => Duration is > 0
            ? TimeSpan.FromSeconds(Duration.Value).ToString(@"hh\:mm\:ss")
            : string.Empty;
    }

    /// <summary>Форматирует длительность для превью.</summary>
    public static class VideoInfoFormatting
    {
        public static string Duration(double? seconds) => seconds is > 0
            ? TimeSpan.FromSeconds(seconds.Value).ToString(@"hh\:mm\:ss")
            : string.Empty;
    }
}
