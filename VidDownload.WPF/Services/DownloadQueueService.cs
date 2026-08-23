using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using VidDownload.WPF.Resources;

namespace VidDownload.WPF.Services
{
    /// <summary>
    /// Очередь загрузок. Управляет жизненным циклом элементов: постановка,
    /// запуск (до MaxConcurrent одновременно), пауза (kill процесса с сохранением
    /// .part-файла), резюме (перезапуск — yt-dlp продолжает докачку), отмена.
    /// Все операции выполняются в UI-потоке, поэтому состояние гонок не имеет.
    /// </summary>
    public class DownloadQueueService : IDownloadQueueService
    {
        private readonly IYtDlpService _ytDlpService;

        public ObservableCollection<DownloadItem> Items { get; } = new();

        public int MaxConcurrent { get; set; } = 1;

        public bool HasActiveDownloads => Items.Any(i => i.Status == DownloadItemStatus.Downloading);

        public event EventHandler<DownloadItem>? ItemStarted;
        public event EventHandler<DownloadItem>? ItemCompleted;
        public event EventHandler<DownloadItem>? ItemFailed;
        public event EventHandler<DownloadItem>? ItemCancelled;

        public DownloadQueueService(IYtDlpService ytDlpService)
        {
            _ytDlpService = ytDlpService;
        }

        public void Enqueue(DownloadItem item)
        {
            Items.Add(item);
            Pump();
        }

        public void Pause(DownloadItem item)
        {
            switch (item.Status)
            {
                case DownloadItemStatus.Downloading:
                    item.PauseRequested = true;
                    item.Cts?.Cancel();
                    break;
                case DownloadItemStatus.Queued:
                    item.Status = DownloadItemStatus.Paused;
                    break;
            }
        }

        public void Resume(DownloadItem item)
        {
            if (item.Status != DownloadItemStatus.Paused)
                return;

            item.PauseRequested = false;
            item.Status = DownloadItemStatus.Queued;
            Pump();
        }

        public void Cancel(DownloadItem item)
        {
            switch (item.Status)
            {
                case DownloadItemStatus.Queued:
                    item.Status = DownloadItemStatus.Cancelled;
                    ItemCancelled?.Invoke(this, item);
                    break;
                case DownloadItemStatus.Downloading:
                    item.Cts?.Cancel();
                    break;
                case DownloadItemStatus.Paused:
                    item.Status = DownloadItemStatus.Cancelled;
                    ItemCancelled?.Invoke(this, item);
                    break;
            }
        }

        public void Remove(DownloadItem item)
        {
            if (item.Status == DownloadItemStatus.Downloading)
                item.Cts?.Cancel();
            Items.Remove(item);
        }

        public void ClearFinished()
        {
            foreach (var item in Items.Where(i => i.IsFinished).ToList())
            {
                Items.Remove(item);
            }
        }

        /// <summary>Останавливает все активные и стоящие в очереди загрузки (выход из приложения).</summary>
        public void CancelAll()
        {
            foreach (var item in Items.Where(i => !i.IsFinished).ToList())
            {
                Cancel(item);
            }
        }

        /// <summary>Запускает стоящие в очереди элементы, пока есть свободные слоты.</summary>
        private void Pump()
        {
            int limit = Math.Clamp(MaxConcurrent, 1, 3);
            while (Items.Count(i => i.Status == DownloadItemStatus.Downloading) < limit)
            {
                var next = Items.FirstOrDefault(i => i.Status == DownloadItemStatus.Queued);
                if (next == null)
                    break;
                _ = RunItemAsync(next);
            }
        }

        private async Task RunItemAsync(DownloadItem item)
        {
            item.Started = true;
            item.Status = DownloadItemStatus.Downloading;
            item.StatusMessage = LocalizedStrings.Instance["StatusPreparing"];
            item.Cts = new CancellationTokenSource();
            ItemStarted?.Invoke(this, item);

            var progress = new Progress<DownloadProgress>(p =>
            {
                if (p.DestinationPath != null && item.Status == DownloadItemStatus.Downloading)
                {
                    item.FilePath = p.DestinationPath;
                    if (string.IsNullOrEmpty(item.Title))
                        item.Title = Path.GetFileNameWithoutExtension(p.DestinationPath);
                }
                item.Percent = p.Percent;
                item.Speed = p.Speed;
                item.Eta = p.Eta;
                item.TotalSize = p.TotalSize;
                if (!string.IsNullOrEmpty(p.StatusMessage))
                    item.StatusMessage = p.StatusMessage;
            });

            try
            {
                string savePath = item.Options.SavePath;
                if (!string.IsNullOrEmpty(savePath) && !Directory.Exists(savePath))
                    Directory.CreateDirectory(savePath);

                var result = await _ytDlpService.DownloadAsync(
                    item.Url, item.Options, item.IsPlaylist, item.IsAudioOnly, item.IsReEncode,
                    progress, item.Cts.Token);

                item.FilePath = result.FilePath;
                if (!string.IsNullOrEmpty(result.Title))
                    item.Title = result.Title;
                item.Percent = 100;
                item.Status = DownloadItemStatus.Completed;
                item.StatusMessage = LocalizedStrings.Instance["StatusCompleted"];
                ItemCompleted?.Invoke(this, item);
            }
            catch (OperationCanceledException)
            {
                bool wasPause = item.PauseRequested;
                item.PauseRequested = false;
                if (wasPause)
                {
                    item.Status = DownloadItemStatus.Paused;
                    item.StatusMessage = LocalizedStrings.Instance["StatusPaused"];
                }
                else
                {
                    item.Status = DownloadItemStatus.Cancelled;
                    item.StatusMessage = LocalizedStrings.Instance["StatusCancelled"];
                    ItemCancelled?.Invoke(this, item);
                }
            }
            catch (UnauthorizedAccessException)
            {
                item.ErrorMessage = LocalizedStrings.Instance["NoSaveFolderAccess"];
                item.StatusMessage = item.ErrorMessage;
                item.Status = DownloadItemStatus.Failed;
                ItemFailed?.Invoke(this, item);
            }
            catch (Exception ex)
            {
                item.ErrorMessage = ex.Message;
                item.StatusMessage = ex.Message;
                item.Status = DownloadItemStatus.Failed;
                AppLog.Error(nameof(DownloadQueueService), $"Download failed ({item.Url}): {ex.Message}");
                ItemFailed?.Invoke(this, item);
            }
            finally
            {
                item.Cts?.Dispose();
                item.Cts = null;
                Pump();
            }
        }
    }
}
