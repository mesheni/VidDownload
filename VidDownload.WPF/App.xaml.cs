using System;
using System.Globalization;
using System.IO;
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
                string settingsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory ?? ".", "user_settings.json");
                if (System.IO.File.Exists(settingsPath))
                {
                    var json = System.IO.File.ReadAllText(settingsPath);
                    var doc = System.Text.Json.JsonDocument.Parse(json);
                    if (doc.RootElement.TryGetProperty("Language", out var lang) && lang.GetString() == "en")
                    {
                        Thread.CurrentThread.CurrentUICulture = new CultureInfo("en");
                        Thread.CurrentThread.CurrentCulture = new CultureInfo("en");
                    }

                    if (doc.RootElement.TryGetProperty("Theme", out var theme) && theme.GetString() == "Light")
                    {
                        // Remove dark skin, add light skin
                        for (int i = Current.Resources.MergedDictionaries.Count - 1; i >= 0; i--)
                        {
                            var src = Current.Resources.MergedDictionaries[i].Source?.OriginalString ?? "";
                            if (src.Contains("SkinDark"))
                                Current.Resources.MergedDictionaries.RemoveAt(i);
                        }
                    }
                }
            }
            catch { }

            base.OnStartup(e);
        }
    }
}
