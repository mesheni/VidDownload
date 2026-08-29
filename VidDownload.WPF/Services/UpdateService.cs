using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Octokit;

namespace VidDownload.WPF.Services
{
    public class UpdateService : IUpdateService
    {
        private const string Owner = "yt-dlp";
        private const string Repo = "yt-dlp";
        private const string AssetName = "yt-dlp.exe";
        private static readonly string YtDlpDestPath = Path.Combine(AppPaths.ToolsDir, "yt-dlp.exe");

        private const string AppOwner = "mesheni";
        private const string AppRepo = "VidDownload";
        private const string AppReleasesUrl = "https://github.com/mesheni/VidDownload/releases/latest";

        /// <summary>
        /// Самообновление работает заменой .exe через Updater, поэтому годится только
        /// portable-asset с расширением .exe; MSI для этой цели не подходит.
        /// </summary>
        private const string AppAssetExtension = ".exe";

        private readonly IYtDlpService _ytDlpService;

        public UpdateService(IYtDlpService ytDlpService)
        {
            _ytDlpService = ytDlpService;
        }

        public async Task<UpdateInfo> CheckForUpdateAsync()
        {
            var info = new UpdateInfo();

            if (!await NetworkHelper.IsInternetAvailableAsync().ConfigureAwait(false))
                return info;

            var client = new GitHubClient(new ProductHeaderValue("VidDownload"));
            var latest = await client.Repository.Release.GetLatest(Owner, Repo).ConfigureAwait(false);

            info.Version = latest.TagName;
            info.ReleaseNotes = latest.Body ?? string.Empty;
            info.IsPreRelease = latest.Prerelease;

            foreach (var asset in latest.Assets)
            {
                if (asset.BrowserDownloadUrl.Contains(AssetName))
                {
                    info.DownloadUrl = asset.BrowserDownloadUrl;
                    break;
                }
            }

            string currentVer = await GetCurrentVersionAsync().ConfigureAwait(false);

            if (string.IsNullOrEmpty(currentVer) ||
                !VersionHelper.IsValidDotted(currentVer) ||
                !VersionHelper.IsValidDotted(info.Version) ||
                VersionHelper.CompareDotted(currentVer, info.Version) < 0)
            {
                info.IsUpdateAvailable = true;
            }

            return info;
        }

        public async Task DownloadUpdateAsync(UpdateInfo info, IProgress<DownloadProgress> progress)
        {
            await NetworkHelper.DownloadFileAsync(info.DownloadUrl, YtDlpDestPath, progress, "yt-dlp.exe")
                .ConfigureAwait(false);
        }

        public async Task<string> GetCurrentVersionAsync()
        {
            return await _ytDlpService.GetLocalVersionAsync().ConfigureAwait(false);
        }

        public async Task<AppUpdateInfo> CheckAppUpdateAsync()
        {
            var info = new AppUpdateInfo();

            if (!await NetworkHelper.IsInternetAvailableAsync().ConfigureAwait(false))
                return info;

            try
            {
                var client = new GitHubClient(new ProductHeaderValue("VidDownload"));
                var releases = await client.Repository.Release.GetAll(AppOwner, AppRepo).ConfigureAwait(false);

                foreach (var release in releases)
                {
                    if (release.Prerelease)
                        continue;

                    info.Version = release.TagName.TrimStart('v', 'V');
                    info.ReleaseNotes = release.Body ?? string.Empty;
                    info.IsPreRelease = false;

                    foreach (var asset in release.Assets)
                    {
                        if (asset.Name.EndsWith(AppAssetExtension, StringComparison.OrdinalIgnoreCase) &&
                            asset.Name.Contains("VidDownload", StringComparison.OrdinalIgnoreCase))
                        {
                            info.DownloadUrl = asset.BrowserDownloadUrl;
                        }
                        else if (string.Equals(asset.Name, "Updater.exe", StringComparison.OrdinalIgnoreCase))
                        {
                            info.UpdaterDownloadUrl = asset.BrowserDownloadUrl;
                        }

                        if (!string.IsNullOrEmpty(info.DownloadUrl) &&
                            !string.IsNullOrEmpty(info.UpdaterDownloadUrl))
                        {
                            break;
                        }
                    }
                    break;
                }

                if (string.IsNullOrEmpty(info.Version))
                    return info;

                string currentVer = GetAppVersion();

                if (string.IsNullOrEmpty(currentVer) ||
                    !Version.TryParse(currentVer, out var current) ||
                    !Version.TryParse(info.Version, out var latest) ||
                    current < latest)
                {
                    info.IsUpdateAvailable = true;
                }
            }
            catch (Exception ex)
            {
                // Сетевая ошибка или rate limit — обновление недоступно, логируем для диагностики
                AppLog.Error(nameof(UpdateService), $"App update check failed: {ex.Message}");
            }

            return info;
        }

        public async Task<string> DownloadAppUpdateAsync(AppUpdateInfo info, IProgress<DownloadProgress> progress)
        {
            string tempDir = Path.Combine(Path.GetTempPath(), "VidDownloadUpdate");
            string fileName = Path.GetFileName(new Uri(info.DownloadUrl).AbsolutePath);
            if (string.IsNullOrEmpty(fileName))
                fileName = "VidDownload.WPF.exe";
            string destPath = Path.Combine(tempDir, fileName);

            await NetworkHelper.DownloadFileAsync(info.DownloadUrl, destPath, progress, fileName)
                .ConfigureAwait(false);

            return destPath;
        }

        public async Task<string?> DownloadUpdaterUpdateAsync(AppUpdateInfo info, IProgress<DownloadProgress> progress)
        {
            if (string.IsNullOrEmpty(info.UpdaterDownloadUrl))
                return null;

            try
            {
                string tempDir = Path.Combine(Path.GetTempPath(), "VidDownloadUpdate");
                string destPath = Path.Combine(tempDir, "Updater.exe");
                await NetworkHelper.DownloadFileAsync(info.UpdaterDownloadUrl, destPath, progress, "Updater.exe")
                    .ConfigureAwait(false);
                return destPath;
            }
            catch (Exception ex)
            {
                // Обновление Updater не критично: главное приложение обновится штатно
                AppLog.Error(nameof(UpdateService), $"Updater download failed: {ex.Message}");
                return null;
            }
        }

        public static string ReleasesUrl => AppReleasesUrl;

        private static string GetAppVersion()
        {
            var version = Assembly.GetEntryAssembly()?.GetName()?.Version;
            return version?.ToString() ?? "0.0.0";
        }
    }
}
