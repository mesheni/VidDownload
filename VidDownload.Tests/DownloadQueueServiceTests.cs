using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using VidDownload.WPF.Control;
using VidDownload.WPF.Services;
using Xunit;

namespace VidDownload.Tests
{
    public class DownloadQueueServiceTests
    {
        private sealed class FakeYtDlpService : IYtDlpService
        {
            public Func<CancellationToken, Task<DownloadResult>> Handler { get; set; } =
                _ => Task.FromResult(new DownloadResult());

            public Task<DownloadResult> DownloadAsync(
                string url, Settings settings, bool isPlaylist, bool isAudioOnly, bool isReEncode,
                IProgress<DownloadProgress> progress, CancellationToken cancellationToken)
                => Handler(cancellationToken);

            public Task<string> GetLocalVersionAsync() => Task.FromResult("2025.01.01");

            public Task<VidDownload.WPF.Control.VideoInfo> FetchInfoAsync(
                string url, bool isPlaylist, CancellationToken cancellationToken = default)
                => Task.FromResult(new VidDownload.WPF.Control.VideoInfo());
        }

        private static DownloadItem MakeItem(string url = "https://youtu.be/x") =>
            new(url, new Settings { SavePath = Path.GetTempPath() }, isPlaylist: false, isAudioOnly: false, isReEncode: false);

        private static Task WaitAsync(TaskCompletionSource tcs, int timeoutMs = 5000) =>
            tcs.Task.WaitAsync(TimeSpan.FromMilliseconds(timeoutMs));

        [Fact]
        public async Task Enqueue_StartsAndCompletes()
        {
            var ytDlp = new FakeYtDlpService
            {
                Handler = _ => Task.FromResult(new DownloadResult { FilePath = @"C:\v\a.mp4", Title = "a" })
            };
            var queue = new DownloadQueueService(ytDlp);

            var completed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            DownloadItem finished = null!;
            queue.ItemCompleted += (_, item) => { finished = item; completed.SetResult(); };

            var item = MakeItem();
            queue.Enqueue(item);

            await WaitAsync(completed);

            Assert.Equal(DownloadItemStatus.Completed, finished.Status);
            Assert.Equal(@"C:\v\a.mp4", finished.FilePath);
            Assert.Equal("a", finished.Title);
            Assert.False(queue.HasActiveDownloads);
        }

        [Fact]
        public async Task Pause_DownloadingItem_MarksPaused()
        {
            var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var ytDlp = new FakeYtDlpService
            {
                Handler = async ct =>
                {
                    started.SetResult();
                    await Task.Delay(Timeout.Infinite, ct);
                    return new DownloadResult();
                }
            };
            var queue = new DownloadQueueService(ytDlp);

            var item = MakeItem();
            queue.Enqueue(item);

            await WaitAsync(started);
            Assert.Equal(DownloadItemStatus.Downloading, item.Status);

            queue.Pause(item);
            await Task.Delay(100);

            Assert.Equal(DownloadItemStatus.Paused, item.Status);
            Assert.False(queue.HasActiveDownloads);
        }

        [Fact]
        public async Task Resume_PausedItem_RunsAgain()
        {
            var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var ytDlp = new FakeYtDlpService
            {
                Handler = async ct =>
                {
                    started.SetResult();
                    await Task.Delay(Timeout.Infinite, ct);
                    return new DownloadResult();
                }
            };
            var queue = new DownloadQueueService(ytDlp);

            var item = MakeItem();
            queue.Enqueue(item);
            await WaitAsync(started);
            queue.Pause(item);
            await Task.Delay(50);

            // Второй запуск (резюме) завершается успешно
            ytDlp.Handler = _ => Task.FromResult(new DownloadResult());
            var completed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            queue.ItemCompleted += (_, _) => completed.SetResult();

            queue.Resume(item);
            await WaitAsync(completed);

            Assert.Equal(DownloadItemStatus.Completed, item.Status);
        }

        [Fact]
        public void Cancel_QueuedItem_MarksCancelledWithoutRun()
        {
            var ytDlp = new FakeYtDlpService();
            var queue = new DownloadQueueService(ytDlp);

            var first = MakeItem("https://youtu.be/first");
            var blocked = MakeItem("https://youtu.be/second");
            var neverStarted = MakeItem("https://youtu.be/third");

            var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            ytDlp.Handler = async ct =>
            {
                started.SetResult();
                await Task.Delay(Timeout.Infinite, ct);
                return new DownloadResult();
            };

            queue.Enqueue(first);
            queue.Enqueue(blocked);
            queue.Enqueue(neverStarted);

            Assert.Equal(DownloadItemStatus.Queued, neverStarted.Status);

            queue.Cancel(neverStarted);
            Assert.Equal(DownloadItemStatus.Cancelled, neverStarted.Status);
            Assert.False(neverStarted.Started);
        }

        [Fact]
        public async Task MaxConcurrent_One_RunsItemsSequentially()
        {
            int running = 0;
            int maxObserved = 0;
            var gate = new SemaphoreSlim(0);

            var ytDlp = new FakeYtDlpService
            {
                Handler = async ct =>
                {
                    Interlocked.Increment(ref running);
                    maxObserved = Math.Max(maxObserved, running);
                    await gate.WaitAsync(ct);
                    Interlocked.Decrement(ref running);
                    return new DownloadResult();
                }
            };
            var queue = new DownloadQueueService(ytDlp) { MaxConcurrent = 1 };

            var allDone = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            int completedCount = 0;
            queue.ItemCompleted += (_, _) =>
            {
                if (Interlocked.Increment(ref completedCount) == 2)
                    allDone.SetResult();
            };

            queue.Enqueue(MakeItem("https://youtu.be/1"));
            queue.Enqueue(MakeItem("https://youtu.be/2"));

            await Task.Delay(150);
            // Второй не должен стартовать, пока первый заблокирован
            Assert.Equal(1, maxObserved);

            gate.Release(2);
            await WaitAsync(allDone);

            Assert.Equal(1, maxObserved);
        }

        [Fact]
        public async Task Failed_SetsErrorMessage()
        {
            var ytDlp = new FakeYtDlpService
            {
                Handler = _ => throw new Exception("boom")
            };
            var queue = new DownloadQueueService(ytDlp);

            var failed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            queue.ItemFailed += (_, _) => failed.SetResult();

            queue.Enqueue(MakeItem());

            await WaitAsync(failed);

            var item = queue.Items[0];
            Assert.Equal(DownloadItemStatus.Failed, item.Status);
            Assert.Contains("boom", item.ErrorMessage);
        }

        [Fact]
        public void ClearFinished_RemovesOnlyTerminalItems()
        {
            var ytDlp = new FakeYtDlpService
            {
                Handler = _ => Task.FromResult(new DownloadResult())
            };
            var queue = new DownloadQueueService(ytDlp);

            var done = MakeItem("https://youtu.be/done");
            queue.Enqueue(done);

            // Элемент в состоянии «В очереди» не должен удаляться
            var queued = MakeItem("https://youtu.be/queued");
            queue.Items.Add(queued);

            // Ждём завершения первого элемента
            Assert.True(SpinWait.SpinUntil(() => done.Status == DownloadItemStatus.Completed, 5000));

            queue.ClearFinished();

            Assert.Single(queue.Items);
            Assert.DoesNotContain(done, queue.Items);
            Assert.Contains(queued, queue.Items);
        }

        [Fact]
        public void ApplyProgress_PlaylistFields_UpdateItemAndComputeTotalPercent()
        {
            var queue = new DownloadQueueService(new FakeYtDlpService());
            var item = MakeItem();
            item.Status = DownloadItemStatus.Downloading;

            queue.ApplyProgress(item, new DownloadProgress
            {
                PlaylistTitle = "My Mix",
                PlaylistIndex = 1,
                PlaylistCount = 15,
                Percent = 100,
                Speed = "2.00 MiB/s"
            });
            queue.ApplyProgress(item, new DownloadProgress
            {
                DestinationPath = @"C:\v\1- First.mp4",
                PlaylistIndex = 1,
                PlaylistCount = 15,
                Percent = 100
            });

            Assert.True(item.HasPlaylistInfo);
            Assert.Equal("My Mix", item.PlaylistTitle);
            Assert.Equal("My Mix", item.DisplayTitle);
            Assert.Equal("1- First", item.Title);
            Assert.Equal("1/15", item.PlaylistCounterText);
            // Первое видео из 15 скачано полностью: ~6.67% общего прогресса
            Assert.Equal(7, item.TotalPercent);

            // Второе видео: название сбрасывается, проценты начинаются заново
            queue.ApplyProgress(item, new DownloadProgress { PlaylistIndex = 2, PlaylistCount = 15, Percent = 0 });
            Assert.Equal(string.Empty, item.Title);

            queue.ApplyProgress(item, new DownloadProgress
            {
                DestinationPath = @"C:\v\2- Second.mp4",
                PlaylistIndex = 2,
                PlaylistCount = 15,
                Percent = 30
            });

            Assert.Equal("2- Second", item.Title);
            Assert.Equal("My Mix", item.DisplayTitle);
            // Одно видео целиком + 30% второго из 15: ~8.67% → 9
            Assert.Equal(9, item.TotalPercent);
            Assert.Equal(30, item.Percent);
        }

        [Fact]
        public void ApplyProgress_SingleVideo_TotalPercentEqualsPercent()
        {
            var queue = new DownloadQueueService(new FakeYtDlpService());
            var item = MakeItem();
            item.Status = DownloadItemStatus.Downloading;

            queue.ApplyProgress(item, new DownloadProgress { Percent = 42 });

            Assert.False(item.HasPlaylistInfo);
            Assert.Equal(42, item.TotalPercent);
            Assert.Equal(string.Empty, item.PlaylistCounterText);
        }

        [Fact]
        public void PercentChange_WithoutPlaylist_RaisesTotalPercent()
        {
            // Регрессия: раньше уведомление TotalPercent отправлялось только для плейлистов,
            // и прогресс-бар одиночного видео оставался на 0%.
            var item = MakeItem();
            var raised = new System.Collections.Generic.List<string>();
            item.PropertyChanged += (_, e) => raised.Add(e.PropertyName);

            item.Percent = 42;

            Assert.Contains(nameof(DownloadItem.TotalPercent), raised);
            Assert.Equal(42, item.TotalPercent);
        }
    }
}
