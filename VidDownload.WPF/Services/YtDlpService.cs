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
            string ytDlpPath = AppPaths.ResolveToolPath("yt-dlp.exe");
            if (!File.Exists(ytDlpPath))
                throw new InvalidOperationException(LocalizedStrings.Instance["YtDlpMissing"]);

            string logPath = CreateUniqueLogPath();

            List<string> args = isAudioOnly
                ? Command.LoadAudio(settings, url, isPlaylist)
                : Command.LoadVideo(url, settings, isPlaylist, isReEncode);

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
