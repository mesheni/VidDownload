using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using VidDownload.WPF.Services;

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
    }
}
