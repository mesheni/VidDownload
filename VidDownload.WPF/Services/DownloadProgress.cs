namespace VidDownload.WPF.Services
{
    public class DownloadProgress
    {
        public int Percent { get; set; }
        public string StatusMessage { get; set; } = string.Empty;
        public string Speed { get; set; } = "--";
        public string Eta { get; set; } = "--";
        public string TotalSize { get; set; } = "--";
    }
}
