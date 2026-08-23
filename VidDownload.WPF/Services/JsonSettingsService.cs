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
            catch (Exception ex)
            {
                // Повреждённый settings.json не должен молча стирать настройки пользователя
                AppLog.Error(nameof(JsonSettingsService), $"Failed to load settings: {ex.Message}");
                return new UserSettings();
            }
        }

        public async Task SaveAsync(UserSettings settings)
        {
            try
            {
                var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
                await AtomicWriteAsync(SettingsPath, json).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                AppLog.Error(nameof(JsonSettingsService), $"Failed to save settings: {ex.Message}");
            }
        }

        /// <summary>
        /// Атомарная замена: запись во временный файл и FileMove поверх целевого,
        /// чтобы обрыв процесса посреди записи не оставил битый JSON.
        /// </summary>
        internal static async Task AtomicWriteAsync(string path, string content)
        {
            string tempPath = path + ".tmp";
            try
            {
                await File.WriteAllTextAsync(tempPath, content).ConfigureAwait(false);
                File.Move(tempPath, path, overwrite: true);
            }
            finally
            {
                try
                {
                    if (File.Exists(tempPath))
                        File.Delete(tempPath);
                }
                catch
                {
                    // Не критично
                }
            }
        }
    }
}
