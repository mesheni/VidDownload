using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace VidDownload.WPF.Control
{
    public class DownloadService
    {
        public event Action<string>? OutputReceived;
        public event Action<int>? ProgressChanged;
        public event Action<bool, string>? DownloadCompleted;

        public async Task DownloadAsync(string url, Settings settings,
            bool isAudio, bool isPlaylist, bool isRecode,
            CancellationToken cancellationToken = default)
        {
            string dateTime = DateTime.Now.ToString("yyyy-MM-dd HH_mm_ss");
            string logPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "log");
            Directory.CreateDirectory(logPath);
            string logFile = System.IO.Path.Combine(logPath, $"{dateTime}_log.txt");

            bool cancelled = false;
            Process? proc = null;

            try
            {
                if (!File.Exists(@".\yt-dlp.exe"))
                {
                    DownloadCompleted?.Invoke(false, "Не найден yt-dlp.exe. Перезапустите программу для автоматической загрузки.");
                    return;
                }

                using var fs = new FileStream(logFile, FileMode.CreateNew);
                using var sw = new StreamWriter(fs, Encoding.Default);
                object logLock = new();
                object stderrLock = new();
                var stderrLines = new Queue<string>();
                const int maxStderrLines = 3;

                void WriteLog(string message)
                {
                    lock (logLock)
                    {
                        sw.WriteLine(message);
                        sw.Flush();
                    }
                }

                void AddStderrLine(string message)
                {
                    lock (stderrLock)
                    {
                        stderrLines.Enqueue(message);

                        while (stderrLines.Count > maxStderrLines)
                        {
                            stderrLines.Dequeue();
                        }
                    }
                }

                string GetStderrSummary()
                {
                    lock (stderrLock)
                    {
                        return stderrLines.Count > 0
                            ? $": {string.Join(" ", stderrLines)}"
                            : string.Empty;
                    }
                }

                proc = new Process();
                proc.StartInfo.FileName = @".\yt-dlp.exe";
                proc.StartInfo.UseShellExecute = false;
                proc.StartInfo.RedirectStandardOutput = true;
                proc.StartInfo.RedirectStandardError = true;
                proc.StartInfo.CreateNoWindow = true;

                if (isAudio)
                {
                    proc.StartInfo.Arguments = Command.LoadAudio(settings, url, isPlaylist);
                }
                else
                {
                    proc.StartInfo.Arguments = Command.LoadVideo(url, settings, isPlaylist, isRecode);
                }

                proc.OutputDataReceived += (sender, e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data))
                    {
                        OutputReceived?.Invoke(e.Data);
                        WriteLog(e.Data);
                        ProgressChanged?.Invoke((int)Math.Round(ParseLog.Parse(e.Data)));
                    }
                };

                proc.ErrorDataReceived += (sender, e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data))
                    {
                        AddStderrLine(e.Data);
                        WriteLog($"[stderr] {e.Data}");
                    }
                };

                proc.Start();
                proc.BeginOutputReadLine();
                proc.BeginErrorReadLine();

                while (!proc.HasExited)
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        cancelled = true;
                        try
                        {
                            if (!proc.HasExited)
                            {
                                proc.Kill(entireProcessTree: true);
                                await proc.WaitForExitAsync().ConfigureAwait(false);
                            }
                        }
                        catch
                        {
                            // Ignore errors while stopping the downloader on cancellation.
                        }
                        break;
                    }
                    await Task.Delay(100, cancellationToken).ConfigureAwait(false);
                }

                if (cancelled)
                {
                    DownloadCompleted?.Invoke(false, "Загрузка отменена");
                    return;
                }

                proc.WaitForExit();

                if (proc.ExitCode == 0)
                {
                    DownloadCompleted?.Invoke(true, "Загрузка завершена");
                }
                else
                {
                    string stderrSummary = GetStderrSummary();
                    DownloadCompleted?.Invoke(false, $"Ошибка yt-dlp: код {proc.ExitCode}{stderrSummary}");
                }
            }
            catch (OperationCanceledException)
            {
                try
                {
                    if (proc != null && !proc.HasExited)
                    {
                        proc.Kill(entireProcessTree: true);
                        await proc.WaitForExitAsync().ConfigureAwait(false);
                    }
                }
                catch
                {
                    // Ignore errors while stopping the downloader on cancellation.
                }

                DownloadCompleted?.Invoke(false, "Загрузка отменена");
            }
            catch (Exception ex)
            {
                DownloadCompleted?.Invoke(false, $"Ошибка: {ex.Message}");
            }
            finally
            {
                proc?.Dispose();
            }
        }
    }
}
