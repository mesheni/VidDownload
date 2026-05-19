using System.IO;
using System.Windows;

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

            base.OnStartup(e);
        }
    }
}
