using System;
using System.Collections.Generic;
using System.Text.Json;

namespace VidDownload.WPF.Control
{
    /// <summary>
    /// Разбор JSON из `yt-dlp -J` (одиночное видео или плейлист с --flat-playlist).
    /// Толерантен к отсутствующим полям — yt-dlp их не пишет для многих сайтов.
    /// </summary>
    public static class MetadataParser
    {
        public static VideoInfo ParseVideoInfo(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
                throw new ArgumentException("Empty yt-dlp JSON", nameof(json));

            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            var info = new VideoInfo
            {
                Id = GetString(root, "id"),
                Title = GetString(root, "title"),
                Uploader = GetString(root, "uploader"),
                Thumbnail = GetString(root, "thumbnail"),
                WebpageUrl = GetString(root, "webpage_url"),
                Duration = GetNullableDouble(root, "duration")
            };

            if (root.TryGetProperty("formats", out var formatsEl) && formatsEl.ValueKind == JsonValueKind.Array)
            {
                foreach (var f in formatsEl.EnumerateArray())
                {
                    var format = new FormatInfo
                    {
                        FormatId = GetString(f, "format_id"),
                        Ext = GetString(f, "ext"),
                        Resolution = GetString(f, "resolution"),
                        VCodec = GetString(f, "vcodec"),
                        ACodec = GetString(f, "acodec"),
                        Fps = GetNullableDouble(f, "fps"),
                        Filesize = GetNullableLong(f, "filesize") ?? GetNullableLong(f, "filesize_approx"),
                        FormatNote = GetString(f, "format_note")
                    };
                    if (!string.IsNullOrEmpty(format.FormatId))
                        info.Formats.Add(format);
                }
            }

            if (root.TryGetProperty("entries", out var entriesEl) && entriesEl.ValueKind == JsonValueKind.Array)
            {
                int index = 1;
                foreach (var e in entriesEl.EnumerateArray())
                {
                    if (e.ValueKind != JsonValueKind.Object)
                        continue;
                    var entry = new PlaylistEntryInfo
                    {
                        Id = GetString(e, "id"),
                        Title = GetString(e, "title"),
                        Duration = GetNullableDouble(e, "duration"),
                        Url = GetString(e, "url"),
                        Index = (int)(GetNullableDouble(e, "playlist_index") ?? index)
                    };
                    if (string.IsNullOrEmpty(entry.Url) && !string.IsNullOrEmpty(entry.Id))
                        entry.Url = entry.Id;
                    if (!string.IsNullOrEmpty(entry.Url))
                        info.Entries.Add(entry);
                    index++;
                }
            }

            return info;
        }

        /// <summary>Оставляет только форматы, пригодные для ручного выбора: видео с разрешением и аудио.</summary>
        public static List<FormatInfo> GetSelectableVideoFormats(VideoInfo info)
        {
            var result = new List<FormatInfo>();
            foreach (var f in info.Formats)
            {
                if (f.IsAudioOnly)
                    continue;
                if (string.IsNullOrEmpty(f.Resolution) || f.Resolution == "audio only")
                    continue;
                result.Add(f);
            }

            // Лучшие (высшие разрешения) сверху
            result.Sort((a, b) => GetHeight(b).CompareTo(GetHeight(a)));
            return result;
        }

        private static int GetHeight(FormatInfo f)
        {
            var res = f.Resolution ?? string.Empty;
            var parts = res.Split('x');
            return parts.Length == 2 && int.TryParse(parts[1], out int height) ? height : 0;
        }

        private static string GetString(JsonElement el, string name) =>
            el.ValueKind == JsonValueKind.Object &&
            el.TryGetProperty(name, out var v) &&
            v.ValueKind == JsonValueKind.String
                ? v.GetString() ?? string.Empty
                : string.Empty;

        private static double? GetNullableDouble(JsonElement el, string name)
        {
            if (el.ValueKind == JsonValueKind.Object && el.TryGetProperty(name, out var v))
            {
                if (v.ValueKind == JsonValueKind.Number && v.TryGetDouble(out double d))
                    return d;
                if (v.ValueKind == JsonValueKind.String && double.TryParse(v.GetString(), out double p))
                    return p;
            }
            return null;
        }

        private static long? GetNullableLong(JsonElement el, string name)
        {
            if (el.ValueKind == JsonValueKind.Object && el.TryGetProperty(name, out var v))
            {
                if (v.ValueKind == JsonValueKind.Number && v.TryGetInt64(out long l))
                    return l;
            }
            return null;
        }
    }
}
