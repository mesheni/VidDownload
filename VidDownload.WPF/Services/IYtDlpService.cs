using System;
using System.Threading;
using System.Threading.Tasks;
using VidDownload.WPF.Control;

namespace VidDownload.WPF.Services
{
    public interface IYtDlpService
    {
        Task<DownloadResult> DownloadAsync(
            string url,
            Settings settings,
            bool isPlaylist,
            bool isAudioOnly,
            bool isReEncode,
            IProgress<DownloadProgress> progress,
            CancellationToken cancellationToken);

        /// <summary>Метаданные до загрузки (`yt-dlp -J`; для плейлистов — плоский список элементов).</summary>
        Task<VideoInfo> FetchInfoAsync(string url, bool isPlaylist, CancellationToken cancellationToken = default);

        Task<string> GetLocalVersionAsync();
    }
}
