using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;
using Octokit;
using VidDownload.WPF.Resources;

namespace VidDownload.WPF.Services
{
    public class FFmpegService : IFFmpegService
    {
        private const string Owner = "BtbN";
        private const string Repo = "FFmpeg-Builds";
        private const string FfmpegExeName = "ffmpeg.exe";
        private const string VersionFileName = "ffmpeg_version.txt";

        private static readonly string InstallDir = AppDomain.CurrentDomain.BaseDirectory;
        private static readonly string VersionFilePath = Path.Combine(AppPaths.ToolsDir, VersionFileName);

        private static string ResolveFfmpegPath()
        {
            string dataPath = Path.Combine(AppPaths.ToolsDir, FfmpegExeName);
            if (File.Exists(dataPath))
                return dataPath;
            string appPath = Path.Combine(InstallDir, FfmpegExeName);
            return File.Exists(appPath) ? appPath : dataPath;
        }

        public Task<string> GetFFmpegPathAsync()
        {
            string path = ResolveFfmpegPath();
            return Task.FromResult(File.Exists(path) ? path : string.Empty);
        }

        public async Task<string> GetLocalVersionAsync()
        {
            string path = ResolveFfmpegPath();
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
            try
            {
                if (File.Exists(VersionFilePath))
                    return File.ReadAllText(VersionFilePath).Trim();
            }
            catch { }
            return string.Empty;
        }

        private static void StoreTag(string tag)
        {
            try
            {
                File.WriteAllText(VersionFilePath, tag);
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

                // Читаем весь вывод до ожидания завершения, иначе процесс может
                // заблокироваться на записи в заполненный пайп (дедлок)
                string output = await proc.StandardOutput.ReadToEndAsync().ConfigureAwait(false);
                await Task.Run(() => proc.WaitForExit()).ConfigureAwait(false);

                string? firstLine = output.Split('\n', 2)[0].Trim();
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

            if (!await NetworkHelper.IsInternetAvailableAsync().ConfigureAwait(false))
                return info;

            string localVer = await GetLocalVersionAsync().ConfigureAwait(false);
            string storedTag = GetStoredTag();
            info.LocalVersion = localVer;

            try
            {
                var client = new GitHubClient(new ProductHeaderValue("VidDownload"));
                var latest = await client.Repository.Release.GetLatest(Owner, Repo).ConfigureAwait(false);

                info.LatestVersion = latest.TagName;

                var matches = latest.Assets.Where(a =>
                {
                    string name = a.Name;
                    return name.Contains("win64", StringComparison.OrdinalIgnoreCase) &&
                           name.Contains("gpl", StringComparison.OrdinalIgnoreCase) &&
                           name.Contains("shared", StringComparison.OrdinalIgnoreCase) &&
                           name.EndsWith(".zip", StringComparison.OrdinalIgnoreCase);
                }).ToList();

                // master-сборки — ежедневные nightly и могут требовать более новый API
                // драйвера GPU (например, NVENC 13.1), чем установлен у пользователя, —
                // аппаратные кодировщики «сами» перестают работать. Берём стабильную ветку.
                var chosen = matches.FirstOrDefault(a =>
                    !a.Name.Contains("master", StringComparison.OrdinalIgnoreCase))
                    ?? matches.FirstOrDefault();

                if (chosen != null)
                {
                    info.DownloadUrl = chosen.BrowserDownloadUrl;
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
            string tempZip = Path.Combine(AppPaths.ToolsDir, "ffmpeg_update.zip");
            try
            {
                // NetworkHelper проверяет HTTP-статус: при 404/403 исключение выйдет
                // наружу, и существующий ffmpeg.exe не будет повреждён
                await NetworkHelper.DownloadFileAsync(info.DownloadUrl, tempZip, progress, "ffmpeg.zip")
                    .ConfigureAwait(false);

                progress?.Report(new DownloadProgress { Percent = 90, StatusMessage = LocalizedStrings.Instance["ExtractingFFmpeg"] });

                string extractDir = Path.Combine(AppPaths.ToolsDir, "ffmpeg_extract");
                if (Directory.Exists(extractDir))
                    Directory.Delete(extractDir, true);
                Directory.CreateDirectory(extractDir);

                ZipFile.ExtractToDirectory(tempZip, extractDir);

                string[] binDirs = Directory.GetDirectories(extractDir, "bin", SearchOption.AllDirectories);
                if (binDirs.Length == 0)
                    throw new InvalidOperationException(LocalizedStrings.Instance["BinFolderNotFound"]);

                string binDir = binDirs[0];
                foreach (string file in Directory.GetFiles(binDir))
                {
                    string destPath = Path.Combine(AppPaths.ToolsDir, Path.GetFileName(file));
                    File.Copy(file, destPath, overwrite: true);
                }

                string exePath = Path.Combine(AppPaths.ToolsDir, FfmpegExeName);
                if (!File.Exists(exePath))
                    throw new InvalidOperationException(LocalizedStrings.Instance["FFmpegExeNotFound"]);

                StoreTag(info.LatestVersion);

                progress?.Report(new DownloadProgress { Percent = 100, StatusMessage = LocalizedStrings.Instance["FFmpegUpdatedShort"] });
            }
            finally
            {
                try { if (File.Exists(tempZip)) File.Delete(tempZip); } catch { }
                try
                {
                    string extractDir = Path.Combine(AppPaths.ToolsDir, "ffmpeg_extract");
                    if (Directory.Exists(extractDir)) Directory.Delete(extractDir, true);
                }
                catch { }
            }
        }
    }
}
