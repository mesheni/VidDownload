using System;

namespace VidDownload.WPF.Services
{
    public class DownloadHistoryEntry
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Url { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; } = DateTime.Now;
        public DownloadStatus Status { get; set; } = DownloadStatus.Completed;
    }

    public enum DownloadStatus
    {
        Completed,
        Failed,
        Cancelled
    }
}
