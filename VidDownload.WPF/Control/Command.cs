namespace VidDownload.WPF.Control
{
    /// <summary>
    /// Предоставляет функции для создания команд yt-dlp загрузки аудио и видео.
    /// LoadAudio создает команду для загрузки только аудио. LoadVideo создает команду 
    /// для загрузки видео, при необходимости включая перекодирование или ремуксинг и настройку разрешения.
    /// Оба принимают ссылочный URL и настройки, а также обрабатывают плейлисты, если они указаны.
    /// </summary>
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

            _format = settings.Format?.ToLowerInvariant();
            if (_isCheckCoder)
            {
                _format = "--recode-video " + _format;
            }
            else
            {
                if (!string.IsNullOrEmpty(_format))
                {
                    _format = "--remux-video " + _format;
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
