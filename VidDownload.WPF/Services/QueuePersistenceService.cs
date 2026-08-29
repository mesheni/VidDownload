using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using VidDownload.WPF.Control;

namespace VidDownload.WPF.Services
{
    /// <summary>DTO незавершённой загрузки для queue.json.</summary>
    public class QueuedItemDto
    {
        public string Url { get; set; } = string.Empty;

        public Settings Options { get; set; } = new();

        public bool IsPlaylist { get; set; }

        public bool IsAudioOnly { get; set; }

        public bool IsReEncode { get; set; }
    }

    /// <summary>
    /// Сохранение/восстановление незавершённой очереди в queue.json (атомарно, temp+move).
    /// Активные элементы восстанавливаются в состоянии Paused — докачка по команде пользователя.
    /// </summary>
    public static class QueuePersistenceService
    {
        private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

        private static string StorePath => Path.Combine(AppPaths.DataDir, "queue.json");

        public static void Save(IReadOnlyList<QueuedItemDto> items, string? path = null)
        {
            try
            {
                string target = path ?? StorePath;
                if (items.Count == 0)
                {
                    if (File.Exists(target))
                        File.Delete(target);
                    return;
                }

                string json = JsonSerializer.Serialize(items, JsonOptions);
                string tmp = target + ".tmp";
                File.WriteAllText(tmp, json);
                File.Move(tmp, target, overwrite: true);
            }
            catch (Exception ex)
            {
                AppLog.Error(nameof(QueuePersistenceService), $"Save failed: {ex.Message}");
            }
        }

        public static List<QueuedItemDto> Load(string? path = null)
        {
            try
            {
                string target = path ?? StorePath;
                if (!File.Exists(target))
                    return new List<QueuedItemDto>();

                string json = File.ReadAllText(target);
                return JsonSerializer.Deserialize<List<QueuedItemDto>>(json, JsonOptions)
                    ?? new List<QueuedItemDto>();
            }
            catch (Exception ex)
            {
                AppLog.Error(nameof(QueuePersistenceService), $"Load failed: {ex.Message}");
                return new List<QueuedItemDto>();
            }
        }
    }
}
