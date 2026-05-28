using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Octokit;

namespace VidDownload.WPF.Services
{
    public class FFmpegService : IFFmpegService
    {
        private const string Owner = "BtbN";
        private const string Repo = "FFmpeg-Builds";
        private const string AssetName = "ffmpeg-master-latest-win64-gpl.zip";
        private const string FfmpegExeName = "ffmpeg.exe";
        private const string VersionFileName = "ffmpeg_version.txt";
        private static readonly string AppDir = AppDomain.CurrentDomain.BaseDirectory;

        public Task<string> GetFFmpegPathAsync()
        {
            string path = Path.Combine(AppDir, FfmpegExeName);
            return Task.FromResult(File.Exists(path) ? path : string.Empty);
        }

        public async Task<string> GetLocalVersionAsync()
        {
            string path = Path.Combine(AppDir, FfmpegExeName);
            if (!File.Exists(path))
                return string.Empty;

            try
            {
                var versionInfo = FileVersionInfo.GetVersionInfo(path);
                string? fv = versionInfo.FileVersion;
                if (!string.IsNullOrEmpty(fv))
                    return fv;

                return await GetVersionFromProcessAsync(path).ConfigureAwait(false);
            }
            catch
            {
                return string.Empty;
            }
        }

        private static string GetStoredTag()
        {
            string versionFile = Path.Combine(AppDir, VersionFileName);
            try
            {
                if (File.Exists(versionFile))
                    return File.ReadAllText(versionFile).Trim();
            }
            catch { }
            return string.Empty;
        }

        private static void StoreTag(string tag)
        {
            string versionFile = Path.Combine(AppDir, VersionFileName);
            try
            {
                File.WriteAllText(versionFile, tag);
            }
            catch { }
        }

        private static async Task<string> GetVersionFromProcessAsync(string path)
        {
            try
            {
                var psi = new ProcessStartInfo(path, "-version")
                {
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                using var proc = Process.Start(psi);
                if (proc == null)
                    return string.Empty;

                string? firstLine = await proc.StandardOutput.ReadLineAsync().ConfigureAwait(false);
                await proc.WaitForExitAsync().ConfigureAwait(false);

                if (string.IsNullOrEmpty(firstLine))
                    return string.Empty;

                int idx = firstLine.IndexOf("version ", StringComparison.OrdinalIgnoreCase);
                if (idx < 0)
                    return firstLine;

                string after = firstLine[(idx + 8)..].Trim();
                int spaceIdx = after.IndexOf(' ');
                return spaceIdx > 0 ? after[..spaceIdx] : after;
            }
            catch
            {
                return string.Empty;
            }
        }

        public async Task<FFmpegInfo> CheckForUpdateAsync()
        {
            var info = new FFmpegInfo();

            if (!await CheckForInternetConnectionAsync().ConfigureAwait(false))
                return info;

            string localVer = await GetLocalVersionAsync().ConfigureAwait(false);
            string storedTag = GetStoredTag();
            info.LocalVersion = localVer;

            try
            {
                var client = new GitHubClient(new ProductHeaderValue("VidDownload"));
                var latest = await client.Repository.Release.GetLatest(Owner, Repo).ConfigureAwait(false);

                info.LatestVersion = latest.TagName;

                foreach (var asset in latest.Assets)
                {
                    if (asset.BrowserDownloadUrl.Contains(AssetName))
                    {
                        info.DownloadUrl = asset.BrowserDownloadUrl;
                        break;
                    }
                }

                if (string.IsNullOrEmpty(storedTag) || storedTag != latest.TagName)
                {
                    info.IsUpdateAvailable = true;
                }
            }
            catch
            {
            }

            return info;
        }

        public async Task DownloadUpdateAsync(FFmpegInfo info, IProgress<DownloadProgress> progress)
        {
            string tempZip = Path.Combine(AppDir, "ffmpeg_update.zip");
            try
            {
                using var httpClient = new HttpClient();
                httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("VidDownload");

                using var response = await httpClient.GetAsync(new Uri(info.DownloadUrl), HttpCompletionOption.ResponseHeadersRead).ConfigureAwait(false);
                var totalBytes = response.Content.Headers.ContentLength ?? -1;
                using var contentStream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
                using var fileStream = new FileStream(tempZip, System.IO.FileMode.Create, System.IO.FileAccess.Write);

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
                            StatusMessage = $"Загрузка FFmpeg... {totalRead * 100 / totalBytes}%"
                        });
                    }
                }

                progress?.Report(new DownloadProgress { Percent = 90, StatusMessage = "Извлечение ffmpeg.exe..." });

                string extractDir = Path.Combine(AppDir, "ffmpeg_extract");
                if (Directory.Exists(extractDir))
                    Directory.Delete(extractDir, true);
                Directory.CreateDirectory(extractDir);

                ZipFile.ExtractToDirectory(tempZip, extractDir);

                string[] exeFiles = Directory.GetFiles(extractDir, FfmpegExeName, SearchOption.AllDirectories);
                if (exeFiles.Length == 0)
                    throw new InvalidOperationException("ffmpeg.exe не найден в архиве");

                string destPath = Path.Combine(AppDir, FfmpegExeName);
                File.Copy(exeFiles[0], destPath, overwrite: true);

                StoreTag(info.LatestVersion);

                progress?.Report(new DownloadProgress { Percent = 100, StatusMessage = "FFmpeg обновлён" });
            }
            finally
            {
                try { if (File.Exists(tempZip)) File.Delete(tempZip); } catch { }
                try
                {
                    string extractDir = Path.Combine(AppDir, "ffmpeg_extract");
                    if (Directory.Exists(extractDir)) Directory.Delete(extractDir, true);
                }
                catch { }
            }
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
