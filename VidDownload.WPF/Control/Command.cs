using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace VidDownload.WPF.Control
{
    public class Command // Класс для сборки команды для yt-dlp 
    {
        static public string LoadAudio(string acodec, string reference, bool? isPlaylist) // Функция сборки команды для загрузки аудио
        {
            string result;

            if ((bool)isPlaylist)
            {
                result = $"yt-dlp -f \"ba\" -x --audio-format {acodec} -o \"./MyVideos/%(playlist)s/%(playlist_index)s- %(title)s.%(ext)s\" \"{reference}\"";
            } else
            {
                result = $"yt-dlp -f \"ba\" -x --audio-format {acodec} -P \"./MyVideos\" \"{reference}\"";
            }

            return result;
        }

        static public string LoadVideo(string reference, string vcodec, string res = "1080", bool? isPlaylist = null, bool? isCheckCoder = null, string format = null) // Функция сборки команды для загрузки видео
        {
            string _format;
            string result;

            if (isCheckCoder != null)
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

            if ((bool)isPlaylist)
            {
                result = $"yt-dlp{format} -S \"+codec:{vcodec},res:{res},fps\" -o \"./MyVideos/%(playlist)s/%(playlist_index)s- %(title)s.%(ext)s\" \"{reference}\"";
            }
            else
            {
                result = $"yt-dlp{format} -S \"+codec:{vcodec},res:{res},fps\" -P \"./MyVideos\" \"{reference}\"";
            }

            return result;
        }
    }
}
