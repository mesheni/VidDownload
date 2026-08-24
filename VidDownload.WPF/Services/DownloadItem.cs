using System;
using CommunityToolkit.Mvvm.ComponentModel;
using VidDownload.WPF.Control;
using VidDownload.WPF.Resources;

namespace VidDownload.WPF.Services
{
    public enum DownloadItemStatus
    {
        Queued,
        Downloading,
        Paused,
        Completed,
        Failed,
        Cancelled
    }

    /// <summary>
    /// Элемент очереди загрузок. Является observable-моделью для строки списка
    /// очереди в интерфейсе, поэтому содержит UI-хелперы (глифы, флаги команд).
    /// </summary>
    public partial class DownloadItem : ObservableObject
    {
        public Guid Id { get; } = Guid.NewGuid();

        public string Url { get; }

        /// <summary>Снимок настроек на момент добавления в очередь.</summary>
        public Settings Options { get; }

        public bool IsPlaylist { get; }

        public bool IsAudioOnly { get; }

        public bool IsReEncode { get; }

        public DateTime CreatedAt { get; } = DateTime.Now;

        [ObservableProperty]
        private DownloadItemStatus status = DownloadItemStatus.Queued;

        [ObservableProperty]
        private string title = string.Empty;

        [ObservableProperty]
        private string filePath = string.Empty;

        [ObservableProperty]
        private int percent;

        [ObservableProperty]
        private string speed = "--";

        [ObservableProperty]
        private string eta = "--";

        [ObservableProperty]
        private string totalSize = "--";

        [ObservableProperty]
        private string statusMessage = string.Empty;

        [ObservableProperty]
        private string errorMessage = string.Empty;

        [ObservableProperty]
        private string playlistTitle = string.Empty;

        [ObservableProperty]
        private int playlistIndex;

        [ObservableProperty]
        private int playlistCount;

        internal CancellationTokenSource? Cts;

        /// <summary>Устанавливается перед отменой, чтобы отличить паузу от отмены.</summary>
        internal bool PauseRequested;

        /// <summary>Загрузка хотя бы раз запускалась (для записи в историю).</summary>
        internal bool Started;

        /// <summary>yt-dlp сообщил, что качается плейлист (index/count уже известны).</summary>
        public bool HasPlaylistInfo => PlaylistCount > 0;

        public string DisplayTitle => !string.IsNullOrEmpty(PlaylistTitle)
            ? PlaylistTitle
            : !string.IsNullOrEmpty(Title) ? Title : Url;

        /// <summary>Строка «Видео: …» для подзаголовка при скачивании плейлиста.</summary>
        public string CurrentVideoLine => HasPlaylistInfo && !string.IsNullOrEmpty(Title)
            ? string.Format(LocalizedStrings.Instance["VideoOfPlaylist"], Title)
            : string.Empty;

        /// <summary>Счётчик «3/15» рядом с заголовком плейлиста.</summary>
        public string PlaylistCounterText => HasPlaylistInfo ? $"{PlaylistIndex}/{PlaylistCount}" : string.Empty;

        /// <summary>Подпись «видео 47%» — прогресс текущего видео внутри плейлиста.</summary>
        public string VideoPercentText => HasPlaylistInfo
            ? string.Format(LocalizedStrings.Instance["VideoPercentFormat"], Percent)
            : string.Empty;

        /// <summary>
        /// Общий прогресс плейлиста: полностью скачанные видео плюс доля текущего.
        /// Для одиночного видео совпадает с Percent.
        /// </summary>
        public int TotalPercent
        {
            get
            {
                if (!HasPlaylistInfo)
                    return Percent;
                double perVideo = 100.0 / PlaylistCount;
                double total = (PlaylistIndex - 1) * perVideo + Percent * perVideo / 100.0;
                return Math.Clamp((int)Math.Round(total), 0, 100);
            }
        }

        public bool CanPauseResume => Status is DownloadItemStatus.Downloading or DownloadItemStatus.Paused;

        public bool CanCancel => Status is DownloadItemStatus.Queued or DownloadItemStatus.Downloading;

        public bool CanRetry => Status is DownloadItemStatus.Failed or DownloadItemStatus.Cancelled;

        public bool CanOpenLocation => Status == DownloadItemStatus.Completed && !string.IsNullOrEmpty(FilePath);

        public bool IsFinished => Status is DownloadItemStatus.Completed
            or DownloadItemStatus.Failed
            or DownloadItemStatus.Cancelled;

        public string PauseResumeIcon => Status == DownloadItemStatus.Paused ? "\uE768" : "\uE769";

        public DownloadItem(string url, Settings options, bool isPlaylist, bool isAudioOnly, bool isReEncode)
        {
            Url = url;
            Options = options;
            IsPlaylist = isPlaylist;
            IsAudioOnly = isAudioOnly;
            IsReEncode = isReEncode;
        }

        partial void OnStatusChanged(DownloadItemStatus value)
        {
            OnPropertyChanged(nameof(CanPauseResume));
            OnPropertyChanged(nameof(CanCancel));
            OnPropertyChanged(nameof(CanRetry));
            OnPropertyChanged(nameof(CanOpenLocation));
            OnPropertyChanged(nameof(IsFinished));
            OnPropertyChanged(nameof(PauseResumeIcon));
        }

        partial void OnTitleChanged(string value)
        {
            OnPropertyChanged(nameof(DisplayTitle));
            OnPropertyChanged(nameof(CurrentVideoLine));
        }

        partial void OnPlaylistTitleChanged(string value) => OnPropertyChanged(nameof(DisplayTitle));

        partial void OnPlaylistIndexChanged(int value) => RaisePlaylistDerived();

        partial void OnPlaylistCountChanged(int value) => RaisePlaylistDerived();

        partial void OnPercentChanged(int value)
        {
            // TotalPercent равен Percent для одиночного видео, поэтому уведомление нужно всегда
            OnPropertyChanged(nameof(TotalPercent));
            if (HasPlaylistInfo)
                OnPropertyChanged(nameof(VideoPercentText));
        }

        private void RaisePlaylistDerived()
        {
            OnPropertyChanged(nameof(HasPlaylistInfo));
            OnPropertyChanged(nameof(PlaylistCounterText));
            OnPropertyChanged(nameof(CurrentVideoLine));
            OnPropertyChanged(nameof(VideoPercentText));
            OnPropertyChanged(nameof(TotalPercent));
        }
    }
}
