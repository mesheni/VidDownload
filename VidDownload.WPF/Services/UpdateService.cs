using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Octokit;

namespace VidDownload.WPF.Services
{
    public class UpdateService : IUpdateService
    {
        private const string Owner = "yt-dlp";
        private const string Repo = "yt-dlp";
        private const string AssetName = "yt-dlp.exe";
        private const string ExeName = "yt-dlp.exe";
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
            using var fileStream = new FileStream(ExeName, System.IO.FileMode.Create, System.IO.FileAccess.Write);

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
                        StatusMessage = $"Загрузка {ExeName}... {totalRead * 100 / totalBytes}%"
                    });
                }
            }
        }

        public async Task<string> GetCurrentVersionAsync()
        {
            return await _ytDlpService.GetLocalVersionAsync().ConfigureAwait(false);
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
