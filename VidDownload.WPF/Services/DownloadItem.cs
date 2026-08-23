using System;
using CommunityToolkit.Mvvm.ComponentModel;
using VidDownload.WPF.Control;

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

        internal CancellationTokenSource? Cts;

        /// <summary>Устанавливается перед отменой, чтобы отличить паузу от отмены.</summary>
        internal bool PauseRequested;

        /// <summary>Загрузка хотя бы раз запускалась (для записи в историю).</summary>
        internal bool Started;

        public string DisplayTitle => string.IsNullOrEmpty(Title) ? Url : Title;

        public bool CanPauseResume => Status is DownloadItemStatus.Downloading or DownloadItemStatus.Paused;

        public bool CanCancel => Status is DownloadItemStatus.Queued or DownloadItemStatus.Downloading;

        public bool IsFinished => Status is DownloadItemStatus.Completed
            or DownloadItemStatus.Failed
            or DownloadItemStatus.Cancelled;

        public string PauseResumeGlyph => Status == DownloadItemStatus.Paused ? "\u25B6" : "\u23F8";

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
            OnPropertyChanged(nameof(IsFinished));
            OnPropertyChanged(nameof(PauseResumeGlyph));
        }

        partial void OnTitleChanged(string value) => OnPropertyChanged(nameof(DisplayTitle));
    }
}
