using System;
using System.Threading.Tasks;

namespace VidDownload.WPF.Services
{
    public interface IUpdateService
    {
        Task<UpdateInfo> CheckForUpdateAsync();
        Task DownloadUpdateAsync(UpdateInfo info, IProgress<DownloadProgress> progress);
        Task<string> GetCurrentVersionAsync();
    }
}
