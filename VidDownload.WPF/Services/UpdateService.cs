using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;
using Octokit;
using VidDownload.WPF.Resources;
using IOFileMode = System.IO.FileMode;

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
        private const string AppAssetPattern = "VidDownload";

        private readonly IYtDlpService _ytDlpService;

        public UpdateService(IYtDlpService ytDlpService)
        {
            _ytDlpService = ytDlpService;
        }

        public async Task<UpdateInfo> CheckForUpdateAsync()
        {
            var info = new UpdateInfo();

            if (!await CheckForInternetConnectionAsync().ConfigureAwait(false))
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
            string latestVer = info.Version.Replace(".", "");

            if (string.IsNullOrEmpty(currentVer) ||
                !int.TryParse(currentVer.Replace(".", ""), out int currentNum) ||
                !int.TryParse(latestVer, out int latestNum) ||
                currentNum < latestNum)
            {
                info.IsUpdateAvailable = true;
            }

            return info;
        }

        public async Task DownloadUpdateAsync(UpdateInfo info, IProgress<DownloadProgress> progress)
        {
            using var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("VidDownload");

            using var response = await httpClient.GetAsync(info.DownloadUrl, HttpCompletionOption.ResponseHeadersRead).ConfigureAwait(false);
            var totalBytes = response.Content.Headers.ContentLength ?? -1;
            using var contentStream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
            using var fileStream = new FileStream(YtDlpDestPath, IOFileMode.Create, System.IO.FileAccess.Write);

            var buffer = new byte[8192];
            long totalRead = 0;
            int bytesRead;
            while ((bytesRead = await contentStream.ReadAsync(buffer, 0, buffer.Length).ConfigureAwait(false)) > 0)
            {
                await fileStream.WriteAsync(buffer, 0, bytesRead).ConfigureAwait(false);
                totalRead += bytesRead;
                if (totalBytes > 0)
                {
                    progress?.Report(new DownloadProgress
                    {
                        Percent = (int)(totalRead * 100 / totalBytes),
                        StatusMessage = string.Format(LocalizedStrings.Instance["DownloadingProgress"], "yt-dlp.exe", totalRead * 100 / totalBytes)
                    });
                }
            }
        }

        public async Task<string> GetCurrentVersionAsync()
        {
            return await _ytDlpService.GetLocalVersionAsync().ConfigureAwait(false);
        }

        public async Task<AppUpdateInfo> CheckAppUpdateAsync()
        {
            var info = new AppUpdateInfo();

            if (!await CheckForInternetConnectionAsync().ConfigureAwait(false))
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
                        if (asset.BrowserDownloadUrl.Contains(AppAssetPattern) &&
                            (asset.BrowserDownloadUrl.EndsWith(".exe") || asset.BrowserDownloadUrl.EndsWith(".msi")))
                        {
                            info.DownloadUrl = asset.BrowserDownloadUrl;
                            break;
                        }
                    }
                    break;
                }

                if (string.IsNullOrEmpty(info.Version))
                    return info;

                string currentVer = GetAppVersion();
                string latestVer = info.Version.Replace(".", "");

                if (string.IsNullOrEmpty(currentVer) ||
                    !Version.TryParse(currentVer, out var current) ||
                    !Version.TryParse(info.Version, out var latest) ||
                    current < latest)
                {
                    info.IsUpdateAvailable = true;
                }
            }
            catch
            {
                // Network error or rate limit — return no update
            }

            return info;
        }

        public async Task DownloadAppUpdateAsync(AppUpdateInfo info, IProgress<DownloadProgress> progress)
        {
            string tempDir = Path.Combine(Path.GetTempPath(), "VidDownloadUpdate");
            Directory.CreateDirectory(tempDir);

            string fileName = info.DownloadUrl.EndsWith(".msi") ? "VidDownload.msi" : "VidDownload.WPF.exe";
            string destPath = Path.Combine(tempDir, fileName);

            using var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("VidDownload");

            using var response = await httpClient.GetAsync(info.DownloadUrl, HttpCompletionOption.ResponseHeadersRead).ConfigureAwait(false);
            var totalBytes = response.Content.Headers.ContentLength ?? -1;
            using var contentStream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
            using var fileStream = new FileStream(destPath, IOFileMode.Create, System.IO.FileAccess.Write);

            var buffer = new byte[8192];
            long totalRead = 0;
            int bytesRead;
            while ((bytesRead = await contentStream.ReadAsync(buffer, 0, buffer.Length).ConfigureAwait(false)) > 0)
            {
                await fileStream.WriteAsync(buffer, 0, bytesRead).ConfigureAwait(false);
                totalRead += bytesRead;
                if (totalBytes > 0)
                {
                    progress?.Report(new DownloadProgress
                    {
                        Percent = (int)(totalRead * 100 / totalBytes),
                        StatusMessage = string.Format(LocalizedStrings.Instance["DownloadingProgress"], fileName, totalRead * 100 / totalBytes)
                    });
                }
            }
        }

        private static string GetAppVersion()
        {
            var version = Assembly.GetEntryAssembly()?.GetName()?.Version;
            return version?.ToString() ?? "0.0.0";
        }

        private static async Task<bool> CheckForInternetConnectionAsync(int timeoutMs = 1000)
        {
            bool result = false;
            await Task.Run(() =>
            {
                try
                {
                    var request = (HttpWebRequest)WebRequest.Create("http://www.gstatic.com/generate_204");
                    request.KeepAlive = false;
                    request.Timeout = timeoutMs;
                    using (var response = (HttpWebResponse)request.GetResponse())
                        result = true;
                }
                catch
                {
                    result = false;
                }
            }).ConfigureAwait(false);
            return result;
        }
    }
}
