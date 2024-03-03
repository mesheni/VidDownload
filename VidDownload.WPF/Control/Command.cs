namespace VidDownload.WPF.Control
{
    /// <summary>
    /// Класс Command инкапсулирует действие и позволяет вызвать действие с помощью метода Execute.
    /// Это позволяет отделить вызов действия от кода пользовательского интерфейса, такого как обработчики нажатий кнопок.
    /// </summary>

    /// Предоставляет функции для создания командных строк yt-dlp для загрузки аудио и видео.
    /// LoadAudio создает команду для загрузки только аудио. LoadVideo создает команду 
    /// для загрузки видео, при необходимости включая перекодирование или ремуксинг и настройку разрешения.
    /// Оба принимают ссылочный URL и настройки, а также обрабатывают плейлисты, если они указаны.
    public static class Command
    {

        static public string LoadAudio(Settings settings, string reference, bool? isPlaylist) // Функция сборки команды для загрузки аудио
        {
            bool _isPlaylist = isPlaylist ?? false;
            string _acodec = settings.AudioCodec;
            string result;

            if (_isPlaylist)
            {
                result = $"yt-dlp -f \"ba\" -x --audio-format {_acodec} -o \"./MyVideos/%(playlist)s/%(playlist_index)s- %(title)s.%(ext)s\" \"{reference}\"";
            }
            else
            {
                result = $"yt-dlp -f \"ba\" -x --audio-format {_acodec} -P \"./MyVideos\" \"{reference}\"";
            }

            return result;
        }

        static public string LoadVideo(string reference, Settings settings, bool? isPlaylist, bool? isCheckCoder) // Функция сборки команды для загрузки видео
        {
            bool _isPlaylist = isPlaylist ?? false;
            bool _isCheckCoder = isCheckCoder ?? false;

            string _res = settings.Resolution;
            string _vcodec = settings.VideoCodec;
            string _format;
            string result;

            if (_isCheckCoder)
            {
                _format = "--recode-video " + settings.Format;
            }
            else
            {
                if (settings.Format != null)
                {
                    _format = "--remux-video " + settings.Format;
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
