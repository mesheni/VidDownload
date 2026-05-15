using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using VidDownload.WPF.Control;

namespace VidDownload.WPF.QueueWindow
{
    public partial class QueueWindow : System.Windows.Window
    {
        private readonly ObservableCollection<QueueItem> _queueItems = new();
        private readonly Settings _settings;
        private readonly bool _isAudio;
        private readonly bool _isPlaylist;
        private readonly bool _isRecode;
        private CancellationTokenSource? _queueCts;
        private bool _isProcessing;

        public QueueWindow(Settings settings, bool isAudio, bool isPlaylist, bool isRecode)
        {
            InitializeComponent();
            _settings = settings;
            _isAudio = isAudio;
            _isPlaylist = isPlaylist;
            _isRecode = isRecode;

            ListViewQueue.ItemsSource = _queueItems;
            UpdateQueueStatus();
        }

        private void ButAddUrl_Click(object sender, RoutedEventArgs e)
        {
            AddUrl(TextBoxAddUrl.Text.Trim());
        }

        private void TextBoxAddUrl_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Enter)
            {
                AddUrl(TextBoxAddUrl.Text.Trim());
                e.Handled = true;
            }
        }

        private async void AddUrl(string url)
        {
            if (string.IsNullOrEmpty(url))
                return;

            // Split by newlines or semicolons to allow batch paste
            var urls = url.Split(new[] { '\n', ';' }, StringSplitOptions.RemoveEmptyEntries)
                          .Select(u => u.Trim())
                          .Where(u => u.Length > 0)
                          .ToArray();

            foreach (var u in urls)
            {
                var item = new QueueItem(u);
                _queueItems.Add(item);
                _ = FetchVideoInfoAsync(item);
            }

            TextBoxAddUrl.Text = string.Empty;
            UpdateQueueStatus();
        }

        private async Task FetchVideoInfoAsync(QueueItem item)
        {
            try
            {
                var proc = new Process();
                proc.StartInfo.FileName = @".\yt-dlp.exe";
                proc.StartInfo.Arguments = $"--print title --print thumbnail \"{item.Url}\"";
                proc.StartInfo.UseShellExecute = false;
                proc.StartInfo.RedirectStandardOutput = true;
                proc.StartInfo.CreateNoWindow = true;

                proc.Start();
                string output = await proc.StandardOutput.ReadToEndAsync();
                await proc.WaitForExitAsync();

                var lines = output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                if (lines.Length > 0 && !string.IsNullOrEmpty(lines[0]))
                {
                    await Dispatcher.InvokeAsync(() => item.Title = lines[0]);
                }

                if (lines.Length > 1 && !string.IsNullOrEmpty(lines[1]))
                {
                    await Dispatcher.InvokeAsync(() => item.ThumbnailUrl = lines[1]);
                }
            }
            catch
            {
                // If yt-dlp isn't available or fails, keep the URL as title
            }
        }

        private void ButRemoveSelected_Click(object sender, RoutedEventArgs e)
        {
            if (_isProcessing) return;

            var toRemove = ListViewQueue.SelectedItems.Cast<QueueItem>().ToList();
            foreach (var item in toRemove)
                _queueItems.Remove(item);

            UpdateQueueStatus();
        }

        private void ButClear_Click(object sender, RoutedEventArgs e)
        {
            if (_isProcessing) return;
            _queueItems.Clear();
            UpdateQueueStatus();
        }

        private async void ButStartQueue_Click(object sender, RoutedEventArgs e)
        {
            if (_isProcessing || _queueItems.Count == 0)
                return;

            _isProcessing = true;
            _queueCts = new CancellationTokenSource();
            var token = _queueCts.Token;

            ButStartQueue.IsEnabled = false;
            ButStopQueue.IsEnabled = true;
            ButAddUrl.IsEnabled = false;
            TextBoxAddUrl.IsEnabled = false;
            ButRemoveSelected.IsEnabled = false;
            ButClear.IsEnabled = false;

            try
            {
                for (int i = 0; i < _queueItems.Count; i++)
                {
                    if (token.IsCancellationRequested)
                        break;

                    var item = _queueItems[i];
                    item.IsProcessing = true;
                    item.Status = "Загрузка...";
                    TextQueueStatus.Text = $"Загрузка {i + 1} из {_queueItems.Count}";

                    var downloadService = new DownloadService();
                    var tcs = new TaskCompletionSource<bool>();

                    downloadService.ProgressChanged += (progress) =>
                    {
                        Dispatcher.Invoke(() => item.Status = $"Загрузка... {progress}%");
                    };

                    downloadService.DownloadCompleted += (success, message) =>
                    {
                        Dispatcher.Invoke(() =>
                        {
                            item.Status = success ? "✓ Готово" : $"✗ {message}";
                            item.IsProcessing = false;
                        });
                        tcs.TrySetResult(success);
                    };

                    try
                    {
                        var downloadTask = downloadService.DownloadAsync(
                            item.Url, _settings, _isAudio, _isPlaylist, _isRecode, token);

                        await Task.WhenAny(downloadTask, tcs.Task);
                    }
                    catch (OperationCanceledException)
                    {
                        item.Status = "✗ Отменено";
                        item.IsProcessing = false;
                        break;
                    }
                    catch (Exception ex)
                    {
                        item.Status = $"✗ {ex.Message}";
                        item.IsProcessing = false;
                    }

                    if (token.IsCancellationRequested)
                        break;

                    // Settings are captured at queue creation time; no update needed
                }
            }
            finally
            {
                _isProcessing = false;
                _queueCts = null;

                Dispatcher.Invoke(() =>
                {
                    ButStartQueue.IsEnabled = true;
                    ButStopQueue.IsEnabled = false;
                    ButAddUrl.IsEnabled = true;
                    TextBoxAddUrl.IsEnabled = true;
                    ButRemoveSelected.IsEnabled = true;
                    ButClear.IsEnabled = true;
                    TextQueueStatus.Text = _queueItems.All(i => i.Status.StartsWith("✓"))
                        ? "Все загрузки завершены"
                        : "Загрузка завершена с ошибками";
                });
            }
        }

        private void ButStopQueue_Click(object sender, RoutedEventArgs e)
        {
            _queueCts?.Cancel();
            ButStopQueue.IsEnabled = false;
        }

        private void UpdateQueueStatus()
        {
            TextQueueStatus.Text = _queueItems.Count > 0
                ? $"В очереди: {_queueItems.Count} видео"
                : "Очередь пуста";
        }
    }
}
