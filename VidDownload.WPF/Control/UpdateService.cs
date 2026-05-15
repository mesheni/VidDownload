using Octokit;
using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace VidDownload.WPF.Control
{
    public class UpdateService
    {
        public event Action<int>? ProgressChanged;
        public event Action<string>? StatusChanged;
        public event Action<bool, string>? UpdateCompleted;

        private readonly HttpClient _httpClient;

        public UpdateService()
        {
            _httpClient = new HttpClient { Timeout = TimeSpan.FromMinutes(10) };
            _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("VidDownload");
        }

        public async Task<UpdateInfo> CheckForUpdateAsync()
        {
            var client = new GitHubClient(new ProductHeaderValue("VidDownload"));
            var latest = await client.Repository.Release.GetLatest("yt-dlp", "yt-dlp");

            string latestVer = latest.TagName.Replace(".", "");
            bool fileNotFound = !File.Exists("yt-dlp.exe");
            string currentVer = string.Empty;

            if (!fileNotFound)
            {
                var versionInfo = FileVersionInfo.GetVersionInfo("yt-dlp.exe");
                currentVer = versionInfo.FileVersion.Replace(".", "");
            }

            bool needsUpdate = fileNotFound ||
                (int.TryParse(currentVer, out int cur) && int.TryParse(latestVer, out int lat) && cur < lat);

            string? downloadUrl = null;
            if (needsUpdate)
            {
                foreach (var asset in latest.Assets)
                {
                    if (asset.BrowserDownloadUrl.Contains("yt-dlp.exe"))
                    {
                        downloadUrl = asset.BrowserDownloadUrl;
                        break;
                    }
                }
            }

            return new UpdateInfo(needsUpdate, currentVer, latest.TagName, downloadUrl);
        }

        public async Task DownloadUpdateAsync(string downloadUrl)
        {
            StatusChanged?.Invoke("Идет загрузка обновления yt-dlp!");

            try
            {
                using var response = await _httpClient.GetAsync(new Uri(downloadUrl), HttpCompletionOption.ResponseHeadersRead);
                response.EnsureSuccessStatusCode();

                var totalBytes = response.Content.Headers.ContentLength ?? -1L;
                using var contentStream = await response.Content.ReadAsStreamAsync();
                using var fileStream = new FileStream("yt-dlp.exe", System.IO.FileMode.Create, System.IO.FileAccess.Write, System.IO.FileShare.None);

                var buffer = new byte[81920];
                long bytesRead = 0;
                int bytesJustRead;

                while ((bytesJustRead = await contentStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                {
                    await fileStream.WriteAsync(buffer, 0, bytesJustRead);
                    bytesRead += bytesJustRead;

                    if (totalBytes > 0)
                    {
                        ProgressChanged?.Invoke((int)((bytesRead * 100) / totalBytes));
                    }
                }

                UpdateCompleted?.Invoke(true, "Обновление завершено!");
            }
            catch (Exception ex)
            {
                UpdateCompleted?.Invoke(false, $"Ошибка при загрузке: {ex.Message}");
            }
        }

        public void Dispose()
        {
            _httpClient?.Dispose();
        }
    }

    public record UpdateInfo(bool NeedsUpdate, string CurrentVersion, string LatestVersion, string? DownloadUrl);
}
