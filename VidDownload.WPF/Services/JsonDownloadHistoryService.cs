using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace VidDownload.WPF.Services
{
    public class JsonDownloadHistoryService : IDownloadHistoryService
    {
        private static readonly string HistoryPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "VidDownload",
            "download-history.json");

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            WriteIndented = true
        };

        private readonly SemaphoreSlim _lock = new(1, 1);

        public JsonDownloadHistoryService()
        {
            var dir = Path.GetDirectoryName(HistoryPath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }
        }

        public async Task AddEntryAsync(DownloadHistoryEntry entry)
        {
            await _lock.WaitAsync().ConfigureAwait(false);
            try
            {
                var entries = await LoadInternalAsync().ConfigureAwait(false);
                entries.Insert(0, entry);
                await SaveInternalAsync(entries).ConfigureAwait(false);
            }
            finally
            {
                _lock.Release();
            }
        }

        public async Task<List<DownloadHistoryEntry>> GetRecentEntriesAsync(int count = 50)
        {
            await _lock.WaitAsync().ConfigureAwait(false);
            try
            {
                var entries = await LoadInternalAsync().ConfigureAwait(false);
                return entries.Take(count).ToList();
            }
            finally
            {
                _lock.Release();
            }
        }

        public async Task ClearHistoryAsync()
        {
            await _lock.WaitAsync().ConfigureAwait(false);
            try
            {
                await SaveInternalAsync(new List<DownloadHistoryEntry>()).ConfigureAwait(false);
            }
            finally
            {
                _lock.Release();
            }
        }

        public async Task RemoveEntryAsync(Guid id)
        {
            await _lock.WaitAsync().ConfigureAwait(false);
            try
            {
                var entries = await LoadInternalAsync().ConfigureAwait(false);
                entries.RemoveAll(e => e.Id == id);
                await SaveInternalAsync(entries).ConfigureAwait(false);
            }
            finally
            {
                _lock.Release();
            }
        }

        private static async Task<List<DownloadHistoryEntry>> LoadInternalAsync()
        {
            try
            {
                if (!File.Exists(HistoryPath))
                    return new List<DownloadHistoryEntry>();

                var json = await File.ReadAllTextAsync(HistoryPath).ConfigureAwait(false);
                return JsonSerializer.Deserialize<List<DownloadHistoryEntry>>(json) ?? new List<DownloadHistoryEntry>();
            }
            catch
            {
                return new List<DownloadHistoryEntry>();
            }
        }

        private static async Task SaveInternalAsync(List<DownloadHistoryEntry> entries)
        {
            var json = JsonSerializer.Serialize(entries, JsonOptions);
            await File.WriteAllTextAsync(HistoryPath, json).ConfigureAwait(false);
        }
    }
}
