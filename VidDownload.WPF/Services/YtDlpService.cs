using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using VidDownload.WPF.Control;
using VidDownload.WPF.Resources;

namespace VidDownload.WPF.Services
{
    public class YtDlpService : IYtDlpService
    {
        public async Task<DownloadResult> DownloadAsync(
            string url,
            Settings settings,
            bool isPlaylist,
            bool isAudioOnly,
            bool isReEncode,
            IProgress<DownloadProgress> progress,
            CancellationToken cancellationToken)
        {
            return await DownloadAsyncInternal(
                url, settings, isPlaylist, isAudioOnly, isReEncode, progress, cancellationToken,
                useYoutubeFallback: false).ConfigureAwait(false);
        }

        private async Task<DownloadResult> DownloadAsyncInternal(
            string url,
            Settings settings,
            bool isPlaylist,
            bool isAudioOnly,
            bool isReEncode,
            IProgress<DownloadProgress> progress,
            CancellationToken cancellationToken,
            bool useYoutubeFallback)
        {
            string ytDlpPath = AppPaths.ResolveToolPath("yt-dlp.exe");
            if (!File.Exists(ytDlpPath))
                throw new InvalidOperationException(LocalizedStrings.Instance["YtDlpMissing"]);

            string logPath = CreateUniqueLogPath();

            List<string> args = isAudioOnly
                ? Command.LoadAudio(settings, url, isPlaylist)
                : Command.LoadVideo(url, settings, isPlaylist, isReEncode);

            if (useYoutubeFallback)
                args = Command.WithYoutubeClientFallback(args);

            // Последний распарсенный прогресс — нераспознанные строки yt-dlp не должны
            // сбрасывать проценты/скорость в ноль
            var lastProgress = new DownloadProgress();
            string? lastDestination = null;
            var stderrTail = new Queue<string>();

            using (Process proc = new())
            {
                proc.StartInfo.FileName = ytDlpPath;
                proc.StartInfo.UseShellExecute = false;
                proc.StartInfo.RedirectStandardOutput = true;
                proc.StartInfo.RedirectStandardError = true;
                proc.StartInfo.CreateNoWindow = true;

                AddJavaScriptRuntimeArgument(proc);

                foreach (var arg in args)
                {
                    proc.StartInfo.ArgumentList.Add(arg);
                }

                var lockObj = new object();
                using (FileStream fs = new(logPath, FileMode.Create, FileAccess.Write, FileShare.Read))
                using (StreamWriter w = new(fs, Encoding.UTF8))
                {
                    proc.OutputDataReceived += (sender, e) =>
                    {
                        if (string.IsNullOrEmpty(e.Data))
                            return;

                        lock (lockObj)
                        {
                            w.WriteLine(e.Data);
                            w.Flush();
                        }

                        var parsed = ParseLog.ParseProgressLine(e.Data, lastProgress);
                        lastProgress = parsed;
                        if (parsed.DestinationPath != null)
                            lastDestination = parsed.DestinationPath;
                        progress?.Report(parsed);
                    };

                    proc.ErrorDataReceived += (sender, e) =>
                    {
                        if (string.IsNullOrEmpty(e.Data))
                            return;

                        lock (lockObj)
                        {
                            w.WriteLine($"[stderr] {e.Data}");
                            w.Flush();
                        }

                        lock (stderrTail)
                        {
                            stderrTail.Enqueue(e.Data);
                            while (stderrTail.Count > 5)
                                stderrTail.Dequeue();
                        }
                    };

                    proc.Start();
                    proc.BeginOutputReadLine();
                    proc.BeginErrorReadLine();

                    using (cancellationToken.Register(() =>
                    {
                        try
                        {
                            // Kill без предварительной проверки HasExited: процесс может
                            // завершиться между проверкой и Kill (TOCTOU)
                            proc.Kill(true);
                        }
                        catch (InvalidOperationException)
                        {
                            // Процесс уже завершился
                        }
                        catch (Exception ex)
                        {
                            AppLog.Error(nameof(YtDlpService), $"Failed to kill yt-dlp: {ex.Message}");
                        }
                    }))
                    {
                        await Task.Run(() => proc.WaitForExit()).ConfigureAwait(false);
                    }

                    if (cancellationToken.IsCancellationRequested)
                    {
                        throw new OperationCanceledException(cancellationToken);
                    }

                    if (proc.ExitCode != 0)
                    {
                        string stderr;
                        lock (stderrTail)
                        {
                            stderr = string.Join(Environment.NewLine, stderrTail);
                        }
                        string message = string.Format(LocalizedStrings.Instance["YtDlpProcessError"], proc.ExitCode);
                        if (!string.IsNullOrEmpty(stderr))
                            message += Environment.NewLine + stderr;

                        if (isAudioOnly && !useYoutubeFallback && IsYoutubeUrl(url) && IsHttpForbidden(stderr)
                            && string.IsNullOrWhiteSpace(settings.CookiesFromBrowser)
                            && string.IsNullOrWhiteSpace(settings.CookiesFile))
                        {
                            AppLog.Info(nameof(YtDlpService),
                                "YouTube returned HTTP 403; retrying with the android player client.");
                            return await DownloadAsyncInternal(
                                url, settings, isPlaylist, isAudioOnly, isReEncode, progress, cancellationToken,
                                useYoutubeFallback: true).ConfigureAwait(false);
                        }

                        throw new Exception(message);
                    }
                }
            }

            var result = new DownloadResult();
            if (!string.IsNullOrEmpty(lastDestination))
            {
                result.FilePath = lastDestination;
                result.Title = Path.GetFileNameWithoutExtension(lastDestination);
            }
            return result;
        }

        private static bool IsHttpForbidden(string stderr) =>
            stderr.Contains("HTTP Error 403", StringComparison.OrdinalIgnoreCase) ||
            stderr.Contains("403 Forbidden", StringComparison.OrdinalIgnoreCase);

        public static bool CanOfferAlternativeFormat(string url, string error, bool audio, bool playlist) =>
            !audio && !playlist && IsYoutubeUrl(url) && IsHttpForbidden(error ?? string.Empty);

        private static bool IsYoutubeUrl(string url)
        {
            if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
                return false;

            string host = uri.Host;
            return host.Equals("youtu.be", StringComparison.OrdinalIgnoreCase) ||
                host.EndsWith(".youtu.be", StringComparison.OrdinalIgnoreCase) ||
                host.Equals("youtube.com", StringComparison.OrdinalIgnoreCase) ||
                host.EndsWith(".youtube.com", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Получает метаданные до загрузки (`yt-dlp -J`, для плейлистов — --flat-playlist).
        /// Бросает исключение с текстом stderr при ненулевом коде выхода.
        /// </summary>
        public async Task<VideoInfo> FetchInfoAsync(string url, bool isPlaylist, CancellationToken cancellationToken = default, Settings? settings = null)
        {
            string ytDlpPath = AppPaths.ResolveToolPath("yt-dlp.exe");
            if (!File.Exists(ytDlpPath))
                throw new InvalidOperationException(LocalizedStrings.Instance["YtDlpMissing"]);

            List<string> args = Command.FetchInfo(url, isPlaylist, settings);

            using Process proc = new();
            proc.StartInfo.FileName = ytDlpPath;
            proc.StartInfo.UseShellExecute = false;
            proc.StartInfo.RedirectStandardOutput = true;
            proc.StartInfo.RedirectStandardError = true;
            proc.StartInfo.CreateNoWindow = true;

            AddJavaScriptRuntimeArgument(proc);

            foreach (var arg in args)
            {
                proc.StartInfo.ArgumentList.Add(arg);
            }

            proc.Start();

            var stdoutTask = proc.StandardOutput.ReadToEndAsync();
            var stderrTask = proc.StandardError.ReadToEndAsync();

            using (cancellationToken.Register(() =>
            {
                try
                {
                    proc.Kill(true);
                }
                catch (InvalidOperationException)
                {
                }
            }))
            {
                await Task.Run(() => proc.WaitForExit()).ConfigureAwait(false);
            }

            cancellationToken.ThrowIfCancellationRequested();

            if (proc.ExitCode != 0)
            {
                string stderr = (await stderrTask.ConfigureAwait(false)).Trim();
                string message = string.Format(LocalizedStrings.Instance["YtDlpProcessError"], proc.ExitCode);
                if (!string.IsNullOrEmpty(stderr))
                    message += Environment.NewLine + stderr;
                throw new Exception(message);
            }

            string json = await stdoutTask.ConfigureAwait(false);
            return MetadataParser.ParseVideoInfo(json);
        }

        private static void AddJavaScriptRuntimeArgument(Process process)
        {
            string? runtime = JavaScriptRuntimeResolver.ResolveRuntime();
            if (string.IsNullOrEmpty(runtime))
                return;

            process.StartInfo.ArgumentList.Add("--js-runtimes");
            process.StartInfo.ArgumentList.Add(runtime);
        }

        public async Task<string> GetLocalVersionAsync()
        {
            try
            {
                string ytDlpPath = AppPaths.ResolveToolPath("yt-dlp.exe");
                if (!File.Exists(ytDlpPath))
                    return string.Empty;

                using (Process proc = new())
                {
                    proc.StartInfo.FileName = ytDlpPath;
                    proc.StartInfo.Arguments = "--version";
                    proc.StartInfo.UseShellExecute = false;
                    proc.StartInfo.RedirectStandardOutput = true;
                    proc.StartInfo.CreateNoWindow = true;
                    proc.Start();
                    string output = await proc.StandardOutput.ReadToEndAsync().ConfigureAwait(false);
                    await Task.Run(() => proc.WaitForExit()).ConfigureAwait(false);
                    return output.Trim();
                }
            }
            catch
            {
                return string.Empty;
            }
        }

        /// <summary>
        /// Имя лога с точностью до миллисекунд; при коллизии добавляется счётчик,
        /// чтобы две загрузки в одну секунду не падали на FileMode.Create.
        /// </summary>
        private static string CreateUniqueLogPath()
        {
            string baseName = $"{DateTime.Now:yyyy-MM-dd HH_mm_ss_fff}_log";
            string path = Path.Combine(AppPaths.LogsDir, $"{baseName}.txt");
            int suffix = 1;
            while (File.Exists(path))
            {
                path = Path.Combine(AppPaths.LogsDir, $"{baseName}_{suffix++}.txt");
            }
            return path;
        }
    }
}
