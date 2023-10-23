using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace VidDownload.WPF.Control
{
    public static class Command // Класс для сборки команды для yt-dlp 
    {
        static public string LoadAudio(string acodec, string reference, bool? isPlaylist) // Функция сборки команды для загрузки аудио
        {
            bool _isPlaylist = isPlaylist ?? false;
            string _acodec = acodec ?? "mp3";
            string result;

            if (_isPlaylist)
            {
                result = $"yt-dlp -f \"ba\" -x --audio-format {_acodec} -o \"./MyVideos/%(playlist)s/%(playlist_index)s- %(title)s.%(ext)s\" \"{reference}\"";
            } else
            {
                result = $"yt-dlp -f \"ba\" -x --audio-format {_acodec} -P \"./MyVideos\" \"{reference}\"";
            }

            return result;
        }

        static public string LoadVideo(string reference, string vcodec, string res, string format, bool? isPlaylist, bool? isCheckCoder) // Функция сборки команды для загрузки видео
        {
            bool _isPlaylist = isPlaylist ?? false;
            bool _isCheckCoder = isCheckCoder ?? false; 

            string _res = res ?? "2160";
            string _vcodec = vcodec ?? "av01";
            string _format;
            string result;

            if (_isCheckCoder)
            {
                _format = "--recode-video " + format;
            }
            else
            {
                if (format != null)
                {
                    _format = "--remux-video " + format;
                }
                else
                {
                    _format = "";
                }
            }

            if (_isPlaylist)
            {
                result = $"yt-dlp {_format} -S \"+codec:{_vcodec},res:{_res},fps\" -o \"./MyVideos/%(playlist)s/%(playlist_index)s- %(title)s.%(ext)s\" \"{reference}\"";
            }
            else
            {
                result = $"yt-dlp {_format} -S \"+codec:{_vcodec},res:{_res},fps\" -P \"./MyVideos\" \"{reference}\"";
            }

            return result;
        }
    }
}
