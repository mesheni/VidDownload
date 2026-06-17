using System;
using System.Threading;
using System.Threading.Tasks;
using VidDownload.WPF.Control;

namespace VidDownload.WPF.Services
{
    public interface IYtDlpService
    {
        Task DownloadAsync(
            string url,
            Settings settings,
            bool isPlaylist,
            bool isAudioOnly,
            bool isReEncode,
            IProgress<DownloadProgress> progress,
            CancellationToken cancellationToken);

        Task<string> GetLocalVersionAsync();
    }
}
