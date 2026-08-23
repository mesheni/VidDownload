using System;
using System.Diagnostics;
using System.IO;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using VidDownload.WPF.Services;

namespace VidDownload.WPF
{
    public partial class App : Application
    {
        private MainWindow? _mainWindow;

        protected override void OnStartup(StartupEventArgs e)
        {
            DispatcherUnhandledException += (_, ex) =>
                AppLog.Error(nameof(App), $"Unhandled UI exception: {ex.Exception}");

            AppServices.Initialize();

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

        protected override void OnExit(ExitEventArgs e)
        {
            // Останавливаем активные процессы yt-dlp, чтобы не оставлять сирот
            AppServices.ServiceProvider.GetService<IDownloadQueueService>()?.CancelAll();
            AppServices.ServiceProvider.GetService<ITrayService>()?.Dispose();
            AppServices.ServiceProvider.GetService<IClipboardMonitorService>()?.Dispose();
            base.OnExit(e);
        }
    }
}
