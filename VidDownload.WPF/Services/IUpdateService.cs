using System;
using System.Threading.Tasks;

namespace VidDownload.WPF.Services
{
    public interface IUpdateService
    {
        Task<UpdateInfo> CheckForUpdateAsync();
        Task DownloadUpdateAsync(UpdateInfo info, IProgress<DownloadProgress> progress);
        Task<string> GetCurrentVersionAsync();

        Task<AppUpdateInfo> CheckAppUpdateAsync();

        /// <summary>Возвращает путь к скачанному файлу обновления приложения.</summary>
        Task<string> DownloadAppUpdateAsync(AppUpdateInfo info, IProgress<DownloadProgress> progress);

        /// <summary>Скачивает новый Updater.exe из релиза (если ассет есть).</summary>
        Task<string?> DownloadUpdaterUpdateAsync(AppUpdateInfo info, IProgress<DownloadProgress> progress);
    }
}
