using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace VidDownload.WPF.Services
{
    public interface IDownloadQueueService
    {
        ObservableCollection<DownloadItem> Items { get; }

        /// <summary>Максимум одновременных загрузок (1–3).</summary>
        int MaxConcurrent { get; set; }

        bool HasActiveDownloads { get; }

        event EventHandler<DownloadItem>? ItemStarted;
        event EventHandler<DownloadItem>? ItemCompleted;
        event EventHandler<DownloadItem>? ItemFailed;
        event EventHandler<DownloadItem>? ItemCancelled;

        void Enqueue(DownloadItem item);
        void Pause(DownloadItem item);
        void Resume(DownloadItem item);
        void Cancel(DownloadItem item);
        void Remove(DownloadItem item);
        void ClearFinished();
        void CancelAll();

        /// <summary>Сохраняет незавершённые элементы в queue.json.</summary>
        void SavePending();

        /// <summary>Восстанавливает сохранённую очередь (все элементы — в паузе).</summary>
        List<DownloadItem> LoadPending();
    }
}
