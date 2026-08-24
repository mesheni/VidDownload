namespace VidDownload.WPF.Services
{
    public class DownloadProgress
    {
        public int Percent { get; set; }
        public string StatusMessage { get; set; } = string.Empty;
        public string Speed { get; set; } = "--";
        public string Eta { get; set; } = "--";
        public string TotalSize { get; set; } = "--";

        /// <summary>
        /// Заполняется строками "[download] Destination: ..." / "[Merger] Merging formats into ...":
        /// путь к итоговому файлу, из которого берутся название видео для истории
        /// и FilePath. null, если строка не содержит назначения.
        /// </summary>
        public string? DestinationPath { get; set; }

        /// <summary>Номер текущего видео в плейлисте ("[download] Downloading item N of M"). null, если данных нет.</summary>
        public int? PlaylistIndex { get; set; }

        /// <summary>Общее число видео в плейлисте. null, если данных нет.</summary>
        public int? PlaylistCount { get; set; }

        /// <summary>Название плейлиста ("[download] Downloading playlist: ..."). null, если данных нет.</summary>
        public string? PlaylistTitle { get; set; }
    }
}
