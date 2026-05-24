using System.IO;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using VidDownload.WPF.Services;

namespace VidDownload.WPF
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            string videoPath = @".\MyVideos\";
            string logPath = @".\log\";
            if (!Directory.Exists(videoPath))
                Directory.CreateDirectory(videoPath);
            if (!Directory.Exists(logPath))
                Directory.CreateDirectory(logPath);

            AppServices.Initialize();

            var mainWindow = AppServices.ServiceProvider.GetRequiredService<MainWindow>();
            mainWindow.Show();

            base.OnStartup(e);
        }
    }
}
