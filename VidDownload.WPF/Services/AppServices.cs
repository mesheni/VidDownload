using System;
using Microsoft.Extensions.DependencyInjection;
using VidDownload.WPF.ConvertWindow;
using VidDownload.WPF.Help;
using VidDownload.WPF.Resources;
using VidDownload.WPF.ViewModels;

namespace VidDownload.WPF.Services
{
    public static class AppServices
    {
        public static IServiceProvider ServiceProvider { get; private set; } = null!;

        public static void Initialize()
        {
            var services = new ServiceCollection();

            var localizedStrings = LocalizedStrings.Instance;
            services.AddSingleton<ILocalizationService>(localizedStrings);
            services.AddSingleton(localizedStrings);

            services.AddSingleton<IYtDlpService, YtDlpService>();
            services.AddSingleton<IDownloadQueueService, DownloadQueueService>();
            services.AddSingleton<IUpdateService, UpdateService>();
            services.AddSingleton<IFFmpegService, FFmpegService>();
            services.AddSingleton<ISettingsService, JsonSettingsService>();
            services.AddSingleton<IMessageService, FluentMessageService>();
            services.AddSingleton<IDialogService, FluentDialogService>();
            services.AddSingleton<IDownloadHistoryService, JsonDownloadHistoryService>();
            services.AddSingleton<IClipboardMonitorService, ClipboardMonitorService>();
            services.AddSingleton<ITrayService, TrayService>();

            // WPF-UI сервисы представления
            services.AddSingleton<Wpf.Ui.ISnackbarService, Wpf.Ui.SnackbarService>();
            services.AddSingleton<Wpf.Ui.IContentDialogService, Wpf.Ui.ContentDialogService>();

            services.AddSingleton(sp => new SnackbarNotificationService(
                sp.GetRequiredService<Wpf.Ui.ISnackbarService>(),
                sp.GetRequiredService<Wpf.Ui.IContentDialogService>(),
                localizedStrings,
                new Lazy<ITrayService>(() => sp.GetRequiredService<ITrayService>())));
            services.AddSingleton<INotificationService>(sp =>
                sp.GetRequiredService<SnackbarNotificationService>());

            services.AddTransient<MainViewModel>();
            services.AddTransient<ConvertViewModel>();
            services.AddTransient<HistoryViewModel>();

            services.AddTransient<VidDownload.WPF.MainWindow>();
            services.AddTransient<ConvertWindow.ConvertWindow>();
            services.AddTransient<HistoryWindow.HistoryWindow>();
            services.AddTransient<HelpWindow>();

            ServiceProvider = services.BuildServiceProvider();
        }
    }
}
