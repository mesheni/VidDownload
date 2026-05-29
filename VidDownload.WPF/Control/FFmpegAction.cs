namespace VidDownload.WPF.Control
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using VidDownload.WPF.Resources;
    using VidDownload.WPF.Services;
    using Xabe.FFmpeg;

    internal class FFmpegAction
    {
        public async Task<string?> ConvertVideoAsync(
            ConversionOptions options,
            IProgress<DownloadProgress>? progress = null,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(options.InputPath) || !File.Exists(options.InputPath))
            {
                progress?.Report(new DownloadProgress
                {
                    Percent = 0,
                    StatusMessage = string.Format(LocalizedStrings.Instance["InputFileNotFound"], options.InputPath)
                });
                return null;
            }

            try
            {
                IMediaInfo mediaInfo = await FFmpeg.GetMediaInfo(options.InputPath, cancellationToken).ConfigureAwait(false);
                cancellationToken.ThrowIfCancellationRequested();

                var conversion = FFmpeg.Conversions.New();

                foreach (var stream in mediaInfo.Streams)
                {
                    conversion.AddStream(stream);
                }

                conversion.SetOutput(options.OutputPath);

                var parameters = BuildConversionParameters(options);
                foreach (var param in parameters)
                {
                    conversion.AddParameter(param);
                }

                cancellationToken.ThrowIfCancellationRequested();

                conversion.OnProgress += (sender, args) =>
                {
                    int percent = (int)Math.Round(args.Duration.TotalSeconds / args.TotalLength.TotalSeconds * 100, 0);
                    Debug.WriteLine($"[{args.Duration} / {args.TotalLength}] {percent}%");
                    progress?.Report(new DownloadProgress
                    {
                        Percent = percent,
                        StatusMessage = $"[{args.Duration} / {args.TotalLength}] {percent}%"
                    });
                };

                await conversion.Start(cancellationToken).ConfigureAwait(false);

                return options.OutputPath;
            }
            catch (OperationCanceledException)
            {
                progress?.Report(new DownloadProgress
                {
                    Percent = 0,
                    StatusMessage = LocalizedStrings.Instance["DownloadCancelled"]
                });
                return null;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"FFmpeg error: {ex}");
                return null;
            }
        }

        public static List<string> BuildConversionParameters(ConversionOptions options)
        {
            var parameters = new List<string>();

            parameters.Add($"-c:v {options.VideoCodec}");
            parameters.Add($"-c:a {options.AudioCodec}");

            if (!string.IsNullOrEmpty(options.Preset) && options.Preset != "medium")
            {
                parameters.Add($"-preset {options.Preset}");
            }

            if (options.Crf.HasValue)
            {
                parameters.Add($"-crf {options.Crf.Value}");
            }
            else if (options.VideoBitrate.HasValue && options.VideoBitrate.Value > 0)
            {
                parameters.Add($"-b:v {options.VideoBitrate.Value}k");
            }

            if (options.AudioBitrate.HasValue && options.AudioBitrate.Value > 0)
            {
                parameters.Add($"-b:a {options.AudioBitrate.Value}k");
            }

            return parameters;
        }

        public static string BuildCommandPreview(ConversionOptions options)
        {
            var sb = new StringBuilder();
            sb.Append("ffmpeg -i \"");
            sb.Append(options.InputPath);
            sb.Append("\" ");

            var parameters = BuildConversionParameters(options);
            foreach (var param in parameters)
            {
                sb.Append(param);
                sb.Append(' ');
            }

            sb.Append('"');
            sb.Append(options.OutputPath);
            sb.Append('"');

            return sb.ToString();
        }

        public static async Task<HashSet<string>> GetAvailableEncodersAsync(string? ffmpegPath = null)
        {
            var available = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            string path = ffmpegPath ?? FindFfmpegPath();
            if (string.IsNullOrEmpty(path) || !File.Exists(path))
                return available;

            try
            {
                var psi = new ProcessStartInfo(path, "-encoders")
                {
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using var proc = Process.Start(psi);
                if (proc == null)
                    return available;

                string output = await proc.StandardOutput.ReadToEndAsync().ConfigureAwait(false);
                await proc.WaitForExitAsync().ConfigureAwait(false);

                foreach (var line in output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    string trimmed = line.Trim();
                    if (trimmed.StartsWith("V") || trimmed.StartsWith("A"))
                    {
                        string[] parts = trimmed.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                        if (parts.Length >= 2)
                        {
                            available.Add(parts[1]);
                        }
                    }
                }
            }
            catch
            {
            }

            return available;
        }

        public static async Task<List<string>> GetFilteredHardwareEncodersAsync(
            string hardwareEncoder,
            string outputFormat)
        {
            var availableHwEncoders = await GetAvailableEncodersAsync().ConfigureAwait(false);
            var filtered = new List<string>();

            var candidates = ConversionOptions.GetVideoCodecsForHardwareEncoder(hardwareEncoder);
            var formatCodecs = ConversionOptions.GetVideoCodecsForFormat(outputFormat);
            var formatSet = new HashSet<string>(formatCodecs, StringComparer.OrdinalIgnoreCase);

            foreach (var codec in candidates)
            {
                if (!formatSet.Contains(codec))
                    continue;

                if (hardwareEncoder == string.Empty)
                {
                    filtered.Add(codec);
                }
                else if (availableHwEncoders.Contains(codec) || availableHwEncoders.Count == 0)
                {
                    filtered.Add(codec);
                }
            }

            if (filtered.Count == 0 && candidates.Count > 0)
            {
                filtered.Add(candidates[0]);
            }

            return filtered;
        }

        public async Task<IMediaInfo?> GetMediaInfoAsync(string filePath)
        {
            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
            {
                return null;
            }

            try
            {
                return await FFmpeg.GetMediaInfo(filePath).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"FFmpeg info error: {ex}");
                return null;
            }
        }

        private static string FindFfmpegPath()
        {
            string toolsPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "VidDownload", "tools", "ffmpeg.exe");
            if (File.Exists(toolsPath))
                return toolsPath;

            string appPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ffmpeg.exe");
            if (File.Exists(appPath))
                return appPath;

            return string.Empty;
        }
    }
}
