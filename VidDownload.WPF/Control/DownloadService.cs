using System;
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

            try
            {
                if (!File.Exists(@".\yt-dlp.exe"))
                {
                    DownloadCompleted?.Invoke(false, "Не найден yt-dlp.exe. Перезапустите программу для автоматической загрузки.");
                    return;
                }

                using var fs = new FileStream(logFile, FileMode.CreateNew);
                using var sw = new StreamWriter(fs, Encoding.Default);

                var proc = new Process();
                proc.StartInfo.FileName = @".\yt-dlp.exe";
                proc.StartInfo.UseShellExecute = false;
                proc.StartInfo.RedirectStandardOutput = true;
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
                        sw.WriteLine(e.Data);
                        sw.Flush();
                        ProgressChanged?.Invoke(ParseLog.Parse(e.Data));
                    }
                };

                proc.Start();
                proc.BeginOutputReadLine();

                await Task.Run(() =>
                {
                    while (!proc.HasExited)
                    {
                        if (cancellationToken.IsCancellationRequested)
                        {
                            proc.Kill();
                            throw new OperationCanceledException(cancellationToken);
                        }
                        proc.WaitForExit(100);
                    }
                }, cancellationToken);

                proc.WaitForExit();
                proc.Close();

                DownloadCompleted?.Invoke(true, "Загрузка завершена");
            }
            catch (OperationCanceledException)
            {
                DownloadCompleted?.Invoke(false, "Загрузка отменена");
            }
            catch (Exception ex)
            {
                DownloadCompleted?.Invoke(false, $"Ошибка: {ex.Message}");
            }
        }
    }
}
