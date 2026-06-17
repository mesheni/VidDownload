using System;
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
            string logPath = AppPaths.LogsDir;

            AppServices.Initialize();

            var mainWindow = AppServices.ServiceProvider.GetRequiredService<MainWindow>();
            mainWindow.Show();

            base.OnStartup(e);
        }
    }
}
