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
        public async Task DownloadAsync(
            string url,
            Settings settings,
            bool isPlaylist,
            bool isAudioOnly,
            bool isReEncode,
            IProgress<DownloadProgress> progress,
            CancellationToken cancellationToken)
        {
            string dateTime = DateTime.Now.ToString("yyyy-MM-dd HH_mm_ss");
            string log = Path.Combine(AppDomain.CurrentDomain.BaseDirectory + @"log\" + dateTime + "_log.txt");
            string? logDir = Path.GetDirectoryName(log);

            if (!string.IsNullOrEmpty(logDir) && !Directory.Exists(logDir))
            {
                Directory.CreateDirectory(logDir);
            }

            List<string> args = isAudioOnly
                ? Command.LoadAudio(settings, url, isPlaylist)
                : Command.LoadVideo(url, settings, isPlaylist, isReEncode);

            using (Process proc = new())
            {
                proc.StartInfo.FileName = @".\yt-dlp.exe";
                proc.StartInfo.UseShellExecute = false;
                proc.StartInfo.RedirectStandardOutput = true;
                proc.StartInfo.CreateNoWindow = true;

                foreach (var arg in args)
                {
                    proc.StartInfo.ArgumentList.Add(arg);
                }

                using (FileStream fs = new(log, FileMode.CreateNew))
                using (StreamWriter w = new(fs, Encoding.Default))
                {
                    proc.OutputDataReceived += (sender, e) =>
                    {
                        if (!string.IsNullOrEmpty(e.Data))
                        {
                            w.WriteLine(e.Data);
                            var parsed = ParseLog.ParseProgressLine(e.Data);
                            progress?.Report(parsed);
                        }
                    };

                    proc.Start();
                    proc.BeginOutputReadLine();

                    using (cancellationToken.Register(() =>
                    {
                        try
                        {
                            if (!proc.HasExited)
                            {
                                proc.Kill(true);
                            }
                        }
                        catch { /* Ignored */ }
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
                        throw new Exception(string.Format(LocalizedStrings.Instance["YtDlpProcessError"], proc.ExitCode));
                    }
                }
            }
        }

        public Task<string> GetLocalVersionAsync()
        {
            try
            {
                if (File.Exists("yt-dlp.exe"))
                {
                    var versionInfo = FileVersionInfo.GetVersionInfo("yt-dlp.exe");
                    return Task.FromResult(versionInfo.FileVersion ?? string.Empty);
                }
            }
            catch
            {
                // Ignored
            }
            return Task.FromResult(string.Empty);
        }
    }
}
