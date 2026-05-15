using System;
using System.Globalization;
using System.Threading;
using System.Windows;
using VidDownload.WPF.Control;

namespace VidDownload.WPF
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            try
            {
                var settings = Settings.Load();
                if (settings.Language == "en")
                {
                    Thread.CurrentThread.CurrentUICulture = new CultureInfo("en");
                    Thread.CurrentThread.CurrentCulture = new CultureInfo("en");
                }
            }
            catch { }

            base.OnStartup(e);
        }
    }
}
