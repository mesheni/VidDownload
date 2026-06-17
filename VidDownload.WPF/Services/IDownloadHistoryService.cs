using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace VidDownload.WPF.Services
{
    public interface IDownloadHistoryService
    {
        Task AddEntryAsync(DownloadHistoryEntry entry);
        Task<List<DownloadHistoryEntry>> GetRecentEntriesAsync(int count = 50);
        Task ClearHistoryAsync();
        Task RemoveEntryAsync(Guid id);
    }
}
