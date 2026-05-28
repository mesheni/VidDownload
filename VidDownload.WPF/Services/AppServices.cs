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
            services.AddSingleton<IUpdateService, UpdateService>();
            services.AddSingleton<IFFmpegService, FFmpegService>();
            services.AddSingleton<ISettingsService, JsonSettingsService>();
            services.AddSingleton<IMessageService, HandyControlMessageService>();
            services.AddSingleton<IDialogService, HandyControlDialogService>();
            services.AddSingleton<IDownloadHistoryService, JsonDownloadHistoryService>();

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
