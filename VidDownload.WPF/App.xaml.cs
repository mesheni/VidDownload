using System;
using System.Globalization;
using System.Threading;
using System.Windows;

namespace VidDownload.WPF
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            try
            {
                string settingsPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "user_settings.json");
                if (System.IO.File.Exists(settingsPath))
                {
                    var json = System.IO.File.ReadAllText(settingsPath);
                    var doc = System.Text.Json.JsonDocument.Parse(json);
                    if (doc.RootElement.TryGetProperty("Language", out var lang) && lang.GetString() == "en")
                    {
                        Thread.CurrentThread.CurrentUICulture = new CultureInfo("en");
                        Thread.CurrentThread.CurrentCulture = new CultureInfo("en");
                    }
                }
            }
            catch { }

            base.OnStartup(e);
        }
    }
}
