using System.Collections.Generic;
using System.IO;

namespace VidDownload.WPF.Control
{
    public static class Command
    {
        public static string HlsFormatSelector(string resolution) =>
            int.TryParse(resolution, out int height) && height > 0
                ? $"b[protocol^=m3u8][height<={height}]"
                : "b[protocol^=m3u8]";

        /// <summary>
        /// Формирует повторную попытку для YouTube с альтернативным клиентом.
        /// Аргумент вставляется перед URL, чтобы он не оказался частью списка ссылок.
        /// </summary>
        public static List<string> WithYoutubeClientFallback(IReadOnlyList<string> args)
        {
            var fallback = new List<string>(args);
            int urlIndex = Math.Max(0, fallback.Count - 1);
            fallback.Insert(urlIndex, "--extractor-args");
            fallback.Insert(urlIndex + 1, "youtube:player_client=android");
            return fallback;
        }

        public static List<string> LoadAudio(Settings settings, string reference, bool? isPlaylist)
        {
            bool _isPlaylist = isPlaylist ?? false;
            string _acodec = settings.AudioCodec;
            var args = new List<string>
            {
                "-f", "ba",
                "-x",
                "--audio-format", _acodec
            };

            if (!string.IsNullOrEmpty(settings.AudioQuality))
            {
                args.Add("--audio-quality");
                args.Add(settings.AudioQuality);
            }

            AppendCommon(args, settings);

            // Галочка «Плейлист» реально управляет скачиванием, а не только раскладкой файлов:
            // без неё даже плейлист-ссылка качается как одиночное видео.
            args.Add(_isPlaylist ? "--yes-playlist" : "--no-playlist");

            AppendPlaylistItems(args, settings, _isPlaylist);
            AppendOutput(args, settings, _isPlaylist);

            args.Add(reference);
            return args;
        }

        public static List<string> LoadVideo(string reference, Settings settings, bool? isPlaylist, bool? isCheckCoder)
        {
            bool _isPlaylist = isPlaylist ?? false;
            bool _isCheckCoder = isCheckCoder ?? false;

            string _res = settings.Resolution;
            string _vcodec = settings.VideoCodec;

            var args = new List<string>();

            // Точный формат из предпросмотра имеет приоритет над сортировкой -S
            if (!string.IsNullOrEmpty(settings.FormatSelector))
            {
                args.Add("-f");
                args.Add(settings.FormatSelector);
            }

            if (_isCheckCoder)
            {
                args.Add("--recode-video");
                args.Add(settings.Format);
            }
            else if (settings.Format != null)
            {
                args.Add("--remux-video");
                args.Add(settings.Format);
            }

            if (string.IsNullOrEmpty(settings.FormatSelector))
            {
                args.Add("-S");
                args.Add($"+codec:{_vcodec},res:{_res},fps");
            }

            AppendCommon(args, settings);

            // Галочка «Плейлист» реально управляет скачиванием, а не только раскладкой файлов:
            // без неё даже плейлист-ссылка качается как одиночное видео.
            args.Add(_isPlaylist ? "--yes-playlist" : "--no-playlist");

            AppendPlaylistItems(args, settings, _isPlaylist);
            AppendOutput(args, settings, _isPlaylist);

            args.Add(reference);
            return args;
        }

        /// <summary>Общие для видео и аудио аргументы: лимит скорости, субтитры, куки, прокси и т.д.</summary>
        private static void AppendCommon(List<string> args, Settings settings)
        {
            if (!string.IsNullOrWhiteSpace(settings.RateLimit))
            {
                args.Add("--limit-rate");
                args.Add(settings.RateLimit.Trim());
            }

            if (settings.DownloadSubtitles)
            {
                args.Add("--write-subs");
                args.Add("--write-auto-subs");
                if (!string.IsNullOrEmpty(settings.SubtitleLanguage))
                {
                    args.Add("--sub-langs");
                    args.Add(settings.SubtitleLanguage);
                }
                if (settings.EmbedSubtitles)
                {
                    args.Add("--embed-subs");
                }
                if (settings.ConvertSubsToSrt)
                {
                    args.Add("--convert-subs");
                    args.Add("srt");
                }
            }

            if (settings.EmbedThumbnail)
                args.Add("--embed-thumbnail");

            if (settings.EmbedMetadata)
                args.Add("--embed-metadata");

            if (!string.IsNullOrWhiteSpace(settings.CookiesFromBrowser))
            {
                args.Add("--cookies-from-browser");
                args.Add(ResolveCookiesBrowser(settings.CookiesFromBrowser.Trim()));
            }
            else if (!string.IsNullOrWhiteSpace(settings.CookiesFile))
            {
                args.Add("--cookies");
                args.Add(settings.CookiesFile.Trim());
            }

            if (!string.IsNullOrWhiteSpace(settings.Proxy))
            {
                args.Add("--proxy");
                args.Add(settings.Proxy.Trim());
            }

            if (settings.Retries > 0)
            {
                args.Add("--retries");
                args.Add(settings.Retries.ToString());
                args.Add("--fragment-retries");
                args.Add(settings.Retries.ToString());
            }

            if (settings.UseDownloadArchive && !string.IsNullOrEmpty(settings.SavePath))
            {
                args.Add("--download-archive");
                args.Add(Path.Combine(settings.SavePath, "downloaded.txt"));
            }

            if (!string.IsNullOrWhiteSpace(settings.DownloadSections))
            {
                args.Add("--download-sections");
                args.Add(settings.DownloadSections.Trim());
            }
        }

        private static string ResolveCookiesBrowser(string browser)
        {
            if (!browser.Equals("waterfox", System.StringComparison.OrdinalIgnoreCase))
                return browser;

            string root = Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.ApplicationData), "Waterfox");
            string ini = Path.Combine(root, "profiles.ini");
            if (!File.Exists(ini))
                throw new System.InvalidOperationException("Waterfox profiles.ini not found: " + ini);

            string section = string.Empty;
            string? installDefault = null;
            string? profilePath = null;
            bool profileDefault = false;
            bool isRelative = true;
            var profiles = new List<(string Path, bool Relative, bool Default)>();
            foreach (string raw in File.ReadAllLines(ini).Append("[End]"))
            {
                string line = raw.Trim();
                if (line.StartsWith("[") && line.EndsWith("]"))
                {
                    if (section.StartsWith("Profile", System.StringComparison.OrdinalIgnoreCase) && !string.IsNullOrEmpty(profilePath))
                        profiles.Add((profilePath, isRelative, profileDefault));
                    section = line[1..^1];
                    profilePath = null;
                    profileDefault = false;
                    isRelative = true;
                }
                else if (line.StartsWith("Default=", System.StringComparison.OrdinalIgnoreCase))
                {
                    string value = line[8..].Trim();
                    if (section.StartsWith("Install", System.StringComparison.OrdinalIgnoreCase) && installDefault == null)
                        installDefault = value;
                    else if (section.StartsWith("Profile", System.StringComparison.OrdinalIgnoreCase))
                        profileDefault = value == "1";
                }
                else if (line.StartsWith("Path=", System.StringComparison.OrdinalIgnoreCase))
                    profilePath = line[5..].Trim();
                else if (line.Equals("IsRelative=0", System.StringComparison.OrdinalIgnoreCase))
                    isRelative = false;
            }

            var selected = profiles.FirstOrDefault(p => installDefault != null && p.Path.Equals(installDefault, System.StringComparison.OrdinalIgnoreCase));
            if (selected.Path == null)
                selected = profiles.FirstOrDefault(p => p.Default);
            if (selected.Path == null && profiles.Count == 1)
                selected = profiles[0];
            if (selected.Path == null)
                throw new System.InvalidOperationException("Default Waterfox profile not found in " + ini);
            string path = selected.Relative ? Path.GetFullPath(Path.Combine(root, selected.Path)) : selected.Path;
            if (!File.Exists(Path.Combine(path, "cookies.sqlite")))
                throw new System.InvalidOperationException("Waterfox cookies database not found in " + path);
            return "firefox:" + path;
        }

        private static void AppendPlaylistItems(List<string> args, Settings settings, bool isPlaylist)
        {
            if (isPlaylist && !string.IsNullOrWhiteSpace(settings.PlaylistItems))
            {
                args.Add("--playlist-items");
                args.Add(settings.PlaylistItems.Trim());
            }
        }

        private static void AppendOutput(List<string> args, Settings settings, bool isPlaylist)
        {
            if (isPlaylist)
            {
                args.Add("-o");
                args.Add($"{settings.SavePath}/%(playlist)s/%(playlist_index)s- %(title)s.%(ext)s");
            }
            else
            {
                args.Add("-P");
                args.Add(settings.SavePath);
            }
        }

        /// <summary>Аргументы запроса метаданных (`yt-dlp -J`) — синхронное поведение с загрузкой.</summary>
        public static List<string> FetchInfo(string reference, bool isPlaylist, Settings? settings = null)
        {
            var args = new List<string>
            {
                "-J",
                isPlaylist ? "--yes-playlist" : "--no-playlist"
            };

            if (isPlaylist)
            {
                // Быстрый список элементов без разбора каждого видео
                args.Add("--flat-playlist");
            }

            if (settings != null)
                AppendCommon(args, new Settings
                {
                    CookiesFromBrowser = settings.CookiesFromBrowser,
                    CookiesFile = settings.CookiesFile,
                    Proxy = settings.Proxy,
                    Retries = settings.Retries
                });
            args.Add(reference);
            return args;
        }
    }
}
