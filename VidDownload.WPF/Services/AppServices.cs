using System;
using Microsoft.Extensions.DependencyInjection;
using VidDownload.WPF.ConvertWindow;
using VidDownload.WPF.Help;
using VidDownload.WPF.ViewModels;

namespace VidDownload.WPF.Services
{
    public static class AppServices
    {
        public static IServiceProvider ServiceProvider { get; private set; } = null!;

        public static void Initialize()
        {
            var services = new ServiceCollection();

            services.AddSingleton<IYtDlpService, YtDlpService>();
            services.AddSingleton<IUpdateService, UpdateService>();
            services.AddSingleton<ISettingsService, JsonSettingsService>();

            services.AddTransient<MainViewModel>();
            services.AddTransient<ConvertViewModel>();

            services.AddTransient<VidDownload.WPF.MainWindow>();
            services.AddTransient<ConvertWindow.ConvertWindow>();
            services.AddTransient<HelpWindow>();

            ServiceProvider = services.BuildServiceProvider();
        }
    }
}
