using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using VidDownload.WPF.Help;
using VidDownload.WPF.Services;
using VidDownload.WPF.ViewModels;

namespace VidDownload.WPF
{
    public partial class App : Application
    {
        private const string SingleInstanceMutexName = @"Local\VidDownload.SingleInstance";
        private const string ActivateSignalName = @"Local\VidDownload.ActivateSignal";

        private MainWindow? _mainWindow;
        private Mutex? _singleInstanceMutex;
        private EventWaitHandle? _activateSignal;

        protected override void OnStartup(StartupEventArgs e)
        {
            string? screenshotPath = null;
            string? targetView = null;
            bool isDemo = false;

            foreach (var arg in e.Args)
            {
                if (arg.StartsWith("--screenshot=", StringComparison.OrdinalIgnoreCase))
                    screenshotPath = arg.Substring("--screenshot=".Length).Trim('"');
                else if (arg.StartsWith("--view=", StringComparison.OrdinalIgnoreCase))
                    targetView = arg.Substring("--view=".Length).Trim('"').ToLowerInvariant();
                else if (arg.Equals("--demo", StringComparison.OrdinalIgnoreCase))
                    isDemo = true;
            }

            if (!string.IsNullOrEmpty(screenshotPath))
            {
                Dispatcher.InvokeAsync(async () =>
                {
                    try
                    {
                        await HandleScreenshotModeAsync(screenshotPath, targetView, isDemo);
                    }
                    catch (Exception ex)
                    {
                        AppLog.Error(nameof(App), $"Screenshot failed: {ex}");
                    }
                    finally
                    {
                        Shutdown();
                    }
                });
                return;
            }

            _singleInstanceMutex = new Mutex(initiallyOwned: true, SingleInstanceMutexName, out bool isFirstInstance);
            if (!isFirstInstance)
            {
                // Вторая копия: просим первую показать окно и тихо выходим
                try
                {
                    using var signal = EventWaitHandle.OpenExisting(ActivateSignalName);
                    signal.Set();
                }
                catch (Exception ex)
                {
                    AppLog.Error(nameof(App), $"Failed to signal first instance: {ex.Message}");
                }
                Shutdown();
                return;
            }

            _activateSignal = new EventWaitHandle(false, EventResetMode.AutoReset, ActivateSignalName);
            var listener = new Thread(WaitForActivateSignal) { IsBackground = true };
            listener.Start();

            CleanupUpdaterLeftovers();

            DispatcherUnhandledException += (_, ex) =>
                AppLog.Error(nameof(App), $"Unhandled UI exception: {ex.Exception}");

            AppServices.Initialize();

            // Применяем сохранённую тему до показа первого окна, чтобы не мигало
            try
            {
                var settings = AppServices.ServiceProvider.GetRequiredService<ISettingsService>()
                    .LoadAsync().GetAwaiter().GetResult();
                UiThemeService.Initialize(UiThemeService.TryParse(settings.Appearance));
            }
            catch (Exception ex)
            {
                AppLog.Error(nameof(App), $"Theme startup failed, using dark: {ex.Message}");
                UiThemeService.Initialize(AppThemePreference.Dark);
            }

            _mainWindow = AppServices.ServiceProvider.GetRequiredService<MainWindow>();
            _mainWindow.Show();

            var tray = AppServices.ServiceProvider.GetRequiredService<ITrayService>();
            tray.Initialize();
            tray.ShowRequested += (_, _) => ShowMainWindow();
            tray.OpenDownloadsRequested += (_, _) => OpenDownloadsFolder();
            tray.ExitRequested += (_, _) => _mainWindow?.ForceExit();

            base.OnStartup(e);
        }

        private void ShowMainWindow()
        {
            if (_mainWindow == null)
                return;
            _mainWindow.Show();
            if (_mainWindow.WindowState == WindowState.Minimized)
                _mainWindow.WindowState = WindowState.Normal;
            _mainWindow.Activate();
        }

        private static void OpenDownloadsFolder()
        {
            try
            {
                var settings = AppServices.ServiceProvider.GetRequiredService<ISettingsService>()
                    .LoadAsync().GetAwaiter().GetResult();
                string path = string.IsNullOrEmpty(settings.SavePath)
                    ? UserSettings.DefaultDownloadPath
                    : settings.SavePath;

                if (!Directory.Exists(path))
                    Directory.CreateDirectory(path);

                Process.Start("explorer.exe", $"\"{path}\"");
            }
            catch (Exception ex)
            {
                AppLog.Error(nameof(App), $"OpenDownloadsFolder failed: {ex.Message}");
            }
        }

        /// <summary>Ждёт сигнал от второй копии и разворачивает окно (в т.ч. из трея).</summary>
        private void WaitForActivateSignal()
        {
            while (_activateSignal is { } signal)
            {
                try
                {
                    if (!signal.WaitOne())
                        return;
                    Dispatcher.BeginInvoke(ShowMainWindow);
                }
                catch (ObjectDisposedException)
                {
                    return;
                }
            }
        }

        /// <summary>Удаляет хвост самообновления Updater.exe.old, если обновлятор не успел.</summary>
        private static void CleanupUpdaterLeftovers()
        {
            try
            {
                string? oldUpdater = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Updater.exe.old");
                if (File.Exists(oldUpdater))
                    File.Delete(oldUpdater);
            }
            catch (Exception ex)
            {
                AppLog.Error(nameof(App), $"Updater cleanup failed: {ex.Message}");
            }
        }

        protected override void OnExit(ExitEventArgs e)
        {
            // Останавливаем активные процессы yt-dlp, чтобы не оставлять сирот
            AppServices.ServiceProvider.GetService<IDownloadQueueService>()?.CancelAll();
            AppServices.ServiceProvider.GetService<ITrayService>()?.Dispose();
            AppServices.ServiceProvider.GetService<IClipboardMonitorService>()?.Dispose();
            _activateSignal?.Dispose();
            _singleInstanceMutex?.Dispose();
            base.OnExit(e);
        }

        private async Task HandleScreenshotModeAsync(string outputPath, string? targetView, bool isDemo)
        {
            ShutdownMode = ShutdownMode.OnExplicitShutdown;
            AppServices.Initialize();
            UiThemeService.Initialize(AppThemePreference.Dark);

            if (targetView == "all")
            {
                string dir = Directory.Exists(outputPath) || !Path.HasExtension(outputPath)
                    ? outputPath
                    : (Path.GetDirectoryName(outputPath) ?? ".");
                Directory.CreateDirectory(dir);

                await CaptureDownloadsAsync(Path.Combine(dir, "screenshot_downloads.png"), isDemo);
                await CaptureConverterAsync(Path.Combine(dir, "screenshot_converter.png"));
                await CaptureHistoryAsync(Path.Combine(dir, "screenshot_history.png"));
                var settingsWin = AppServices.ServiceProvider.GetRequiredService<SettingsWindow.SettingsWindow>();
                if (settingsWin.DataContext is SettingsViewModel svm) { try { await svm.LoadAsync(); } catch { } }
                await CaptureAndSaveAsync(settingsWin, Path.Combine(dir, "screenshot_settings.png"), 520, 720);
                var helpWin = AppServices.ServiceProvider.GetRequiredService<HelpWindow>();
                await CaptureAndSaveAsync(helpWin, Path.Combine(dir, "screenshot_help.png"), 560, 720);
            }
            else if (targetView == "settings")
            {
                var settingsWin = AppServices.ServiceProvider.GetRequiredService<SettingsWindow.SettingsWindow>();
                if (settingsWin.DataContext is SettingsViewModel svm) { try { await svm.LoadAsync(); } catch { } }
                await CaptureAndSaveAsync(settingsWin, outputPath, 520, 720);
            }
            else if (targetView == "help")
            {
                var helpWin = AppServices.ServiceProvider.GetRequiredService<HelpWindow>();
                await CaptureAndSaveAsync(helpWin, outputPath, 560, 720);
            }
            else if (targetView == "history")
            {
                await CaptureHistoryAsync(outputPath);
            }
            else if (targetView == "converter")
            {
                await CaptureConverterAsync(outputPath);
            }
            else
            {
                await CaptureDownloadsAsync(outputPath, isDemo);
            }
        }

        private static async Task CaptureDownloadsAsync(string path, bool isDemo)
        {
            var main = AppServices.ServiceProvider.GetRequiredService<MainWindow>();
            var vm = (MainViewModel)main.DataContext;
            vm.CurrentPage = MainNavigationPage.Downloads;
            vm.Url = string.Empty;
            if (isDemo)
            {
                vm.Queue.Items.Clear();
                var opt = new Control.Settings();
                var item1 = new DownloadItem("https://example.com/watch?v=1", opt.Clone(), true, false, false)
                {
                    Title = "Введение в алгоритмы — полный курс",
                    PlaylistTitle = "Лекция 4 · Сортировки и оценка сложности",
                    PlaylistIndex = 4,
                    PlaylistCount = 12,
                    Status = DownloadItemStatus.Downloading,
                    Speed = "4,2 МБ/с",
                    Eta = "02:14",
                    TotalSize = "612 МБ",
                    Percent = 37
                };
                var item2 = new DownloadItem("https://example.com/watch?v=2", opt.Clone(), false, false, false)
                {
                    Title = "Запись конференции — день 1",
                    Status = DownloadItemStatus.Downloading,
                    Speed = "3,8 МБ/с",
                    Eta = "00:41",
                    TotalSize = "1,2 ГБ",
                    Percent = 71
                };
                var item3 = new DownloadItem("https://example.com/watch?v=3", opt.Clone(), false, false, false)
                {
                    Title = "Подкаст — выпуск 48",
                    Status = DownloadItemStatus.Paused,
                    TotalSize = "86 МБ",
                    Percent = 45
                };
                var item4 = new DownloadItem("https://example.com/watch?v=4", opt.Clone(), false, false, false)
                {
                    Title = "Документальный фильм — 1080p",
                    Status = DownloadItemStatus.Completed,
                    TotalSize = "2,4 ГБ",
                    Percent = 100
                };
                var item5 = new DownloadItem("https://example.com/watch?v=5", opt.Clone(), false, false, false)
                {
                    Title = "Видео недоступно в вашем регионе",
                    Status = DownloadItemStatus.Failed,
                    ErrorMessage = "ERROR: Video unavailable. This video is not available in your country",
                    TotalSize = "—",
                    Percent = 12
                };
                vm.Queue.Items.Add(item1);
                vm.Queue.Items.Add(item2);
                vm.Queue.Items.Add(item3);
                vm.Queue.Items.Add(item4);
                vm.Queue.Items.Add(item5);
                vm.ActiveDownloadsBadgeCount = 2;
                vm.HasActiveBadge = true;
                vm.QueueSummaryText = "2 активных · 1 на паузе";
            }
            if (string.IsNullOrEmpty(vm.FfmpegVersion) || vm.FfmpegVersion.Contains("...") || vm.FfmpegVersion.Contains("провер"))
                vm.FfmpegVersion = "8.1.2";
            await CaptureAndSaveAsync(main, path, 1120, 720);
        }

        private static async Task CaptureConverterAsync(string path)
        {
            var main = AppServices.ServiceProvider.GetRequiredService<MainWindow>();
            var vm = (MainViewModel)main.DataContext;
            vm.CurrentPage = MainNavigationPage.Converter;
            string demoFile = Path.Combine(Path.GetTempPath(), "interview_raw.mov");
            try { if (!File.Exists(demoFile)) File.WriteAllText(demoFile, "demo"); } catch { }
            vm.ConvertVm.FilePath = demoFile;
            vm.ConvertVm.OutputDirectory = @"C:\Users\User\Videos\Converted";
            vm.ConvertVm.OutputFileName = "interview_raw_converted";
            vm.ConvertVm.SelectedFormat = "MP4";
            vm.ConvertVm.SelectedVideoCodec = "libx264";
            vm.ConvertVm.SelectedAudioCodec = "aac";
            vm.ConvertVm.SelectedPreset = "medium";
            vm.ConvertVm.Crf = 23;
            vm.ConvertVm.AudioBitrate = 192;
            vm.ConvertVm.StatusMessage = "Готов к конвертации";
            vm.ConvertVm.FfmpegCommandPreview = "ffmpeg -i interview_raw.mov -c:v libx264 -crf 23 -preset medium -c:a aac -b:a 192k interview_raw_converted.mp4";
            await CaptureAndSaveAsync(main, path, 1120, 720);
        }

        private static async Task CaptureHistoryAsync(string path)
        {
            var historyService = AppServices.ServiceProvider.GetRequiredService<IDownloadHistoryService>();
            try { await historyService.ClearHistoryAsync(); } catch { }
            var demoHistory = new[]
            {
                new DownloadHistoryEntry { Timestamp = DateTime.Parse("2026-11-07 19:42:10"), Title = "Документальный фильм — 1080p", Url = "https://example.com/watch?v=a1b2c3", Status = DownloadStatus.Completed, FilePath = @"C:\Users\User\Videos\doc.mp4" },
                new DownloadHistoryEntry { Timestamp = DateTime.Parse("2026-11-07 18:15:03"), Title = "Запись конференции — день 1", Url = "https://example.com/watch?v=d4e5f6", Status = DownloadStatus.Completed, FilePath = @"C:\Users\User\Videos\conf.mp4" },
                new DownloadHistoryEntry { Timestamp = DateTime.Parse("2026-11-07 16:58:41"), Title = "Видео недоступно в вашем регионе", Url = "https://example.com/watch?v=g7h8i9", Status = DownloadStatus.Failed, FilePath = "" },
                new DownloadHistoryEntry { Timestamp = DateTime.Parse("2026-11-06 22:07:29"), Title = "Введение в алгоритмы — полный курс", Url = "https://example.com/playlist?list=PL0001", Status = DownloadStatus.Completed, FilePath = @"C:\Users\User\Videos\algo.mp4" },
                new DownloadHistoryEntry { Timestamp = DateTime.Parse("2026-11-06 13:30:55"), Title = "Подкаст — выпуск 47", Url = "https://example.com/watch?v=j1k2l3", Status = DownloadStatus.Cancelled, FilePath = "" },
                new DownloadHistoryEntry { Timestamp = DateTime.Parse("2026-11-05 21:12:08"), Title = "Лекция по компьютерным сетям", Url = "https://example.com/watch?v=m4n5o6", Status = DownloadStatus.Completed, FilePath = @"C:\Users\User\Videos\networks.mp4" },
                new DownloadHistoryEntry { Timestamp = DateTime.Parse("2026-11-05 09:44:17"), Title = "Обзор инструментов разработчика", Url = "https://example.com/watch?v=p7q8r9", Status = DownloadStatus.Completed, FilePath = @"C:\Users\User\Videos\tools.mp4" },
                new DownloadHistoryEntry { Timestamp = DateTime.Parse("2026-11-03 20:01:36"), Title = "Концерт — полная запись", Url = "https://example.com/watch?v=s1t2u3", Status = DownloadStatus.Failed, FilePath = "" }
            };
            foreach (var h in demoHistory)
            {
                try { await historyService.AddEntryAsync(h); } catch { }
            }

            var main = AppServices.ServiceProvider.GetRequiredService<MainWindow>();
            var vm = (MainViewModel)main.DataContext;
            vm.CurrentPage = MainNavigationPage.History;
            try { await vm.HistoryVm.LoadAsync(); } catch { }
            await CaptureAndSaveAsync(main, path, 1120, 720);
        }

        private static async Task CaptureAndSaveAsync(Window win, string path, int width, int height)
        {
            win.Width = width;
            win.Height = height;
            win.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            win.Show();

            // Allow layout passes
            win.UpdateLayout();
            await Task.Delay(400);
            win.UpdateLayout();

            int w = (int)win.ActualWidth > 0 ? (int)win.ActualWidth : width;
            int h = (int)win.ActualHeight > 0 ? (int)win.ActualHeight : height;

            var rtb = new System.Windows.Media.Imaging.RenderTargetBitmap(w, h, 96, 96, System.Windows.Media.PixelFormats.Pbgra32);
            rtb.Render(win);

            var encoder = new System.Windows.Media.Imaging.PngBitmapEncoder();
            encoder.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(rtb));

            string? dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            using (var fs = File.Create(path))
            {
                encoder.Save(fs);
            }

            win.Close();
        }
    }
}
