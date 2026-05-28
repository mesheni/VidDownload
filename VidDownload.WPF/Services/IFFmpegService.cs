using System;
using System.Threading.Tasks;

namespace VidDownload.WPF.Services
{
    public interface IFFmpegService
    {
        Task<FFmpegInfo> CheckForUpdateAsync();
        Task DownloadUpdateAsync(FFmpegInfo info, IProgress<DownloadProgress> progress);
        Task<string> GetLocalVersionAsync();
        Task<string> GetFFmpegPathAsync();
    }
}
