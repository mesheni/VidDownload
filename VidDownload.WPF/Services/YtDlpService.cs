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
            string log = Path.Combine(AppPaths.LogsDir, $"{dateTime}_log.txt");

            List<string> args = isAudioOnly
                ? Command.LoadAudio(settings, url, isPlaylist)
                : Command.LoadVideo(url, settings, isPlaylist, isReEncode);

            using (Process proc = new())
            {
                proc.StartInfo.FileName = AppPaths.ResolveToolPath("yt-dlp.exe");
                proc.StartInfo.UseShellExecute = false;
                proc.StartInfo.RedirectStandardOutput = true;
                proc.StartInfo.RedirectStandardError = true;
                proc.StartInfo.CreateNoWindow = true;

                foreach (var arg in args)
                {
                    proc.StartInfo.ArgumentList.Add(arg);
                }

                var lockObj = new object();
                using (FileStream fs = new(log, FileMode.CreateNew))
                using (StreamWriter w = new(fs, Encoding.UTF8))
                {
                    proc.OutputDataReceived += (sender, e) =>
                    {
                        if (!string.IsNullOrEmpty(e.Data))
                        {
                            lock (lockObj)
                            {
                                w.WriteLine(e.Data);
                                w.Flush();
                            }
                            var parsed = ParseLog.ParseProgressLine(e.Data);
                            progress?.Report(parsed);
                        }
                    };

                    proc.ErrorDataReceived += (sender, e) =>
                    {
                        if (!string.IsNullOrEmpty(e.Data))
                        {
                            lock (lockObj)
                            {
                                w.WriteLine($"[stderr] {e.Data}");
                                w.Flush();
                            }
                        }
                    };

                    proc.Start();
                    proc.BeginOutputReadLine();
                    proc.BeginErrorReadLine();

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
    }
}
