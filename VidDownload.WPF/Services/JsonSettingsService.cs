using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace VidDownload.WPF.Services
{
    public class JsonSettingsService : ISettingsService
    {
        private static readonly string SettingsPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "VidDownload",
            "settings.json");

        public JsonSettingsService()
        {
            var dir = Path.GetDirectoryName(SettingsPath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }
        }

        public async Task<UserSettings> LoadAsync()
        {
            try
            {
                if (!File.Exists(SettingsPath))
                    return new UserSettings();

                var json = await File.ReadAllTextAsync(SettingsPath).ConfigureAwait(false);
                return JsonSerializer.Deserialize<UserSettings>(json) ?? new UserSettings();
            }
            catch
            {
                return new UserSettings();
            }
        }

        public async Task SaveAsync(UserSettings settings)
        {
            try
            {
                var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
                await File.WriteAllTextAsync(SettingsPath, json).ConfigureAwait(false);
            }
            catch
            {
                // Ignored
            }
        }
    }
}
