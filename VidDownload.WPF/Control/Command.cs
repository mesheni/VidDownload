using System.Collections.Generic;

namespace VidDownload.WPF.Control
{
    public static class Command
    {
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
            }

            if (_isPlaylist)
            {
                args.Add("-o");
                args.Add("./MyVideos/%(playlist)s/%(playlist_index)s- %(title)s.%(ext)s");
            }
            else
            {
                args.Add("-P");
                args.Add("./MyVideos");
            }

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

            args.Add("-S");
            args.Add($"+codec:{_vcodec},res:{_res},fps");

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
            }

            if (_isPlaylist)
            {
                args.Add("-o");
                args.Add("./MyVideos/%(playlist)s/%(playlist_index)s- %(title)s.%(ext)s");
            }
            else
            {
                args.Add("-P");
                args.Add("./MyVideos");
            }

            args.Add(reference);
            return args;
        }
    }
}
