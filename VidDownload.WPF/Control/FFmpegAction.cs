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
        private static bool _executablesPathConfigured;

        /// <summary>
        /// Направляет Xabe.FFmpeg на локальный ffmpeg из tools\ — сам он ищет бинарники только в PATH.
        /// </summary>
        public static bool EnsureExecutablesPath()
        {
            if (_executablesPathConfigured)
                return true;

            string path = FindFfmpegPath();
            if (string.IsNullOrEmpty(path))
                return false;

            FFmpeg.SetExecutablesPath(Path.GetDirectoryName(path));
            _executablesPathConfigured = true;
            return true;
        }

        /// <summary>Сбрасывает закэшированный путь (после скачивания новой версии FFmpeg).</summary>
        public static void ResetExecutablesPath()
        {
            _executablesPathConfigured = false;
            _hardwareEncoderProbeCache = null;
        }

        public async Task<ConversionResult> ConvertVideoAsync(
            ConversionOptions options,
            IProgress<DownloadProgress>? progress = null,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(options.InputPath) || !File.Exists(options.InputPath))
            {
                string message = string.Format(LocalizedStrings.Instance["InputFileNotFound"], options.InputPath);
                progress?.Report(new DownloadProgress { Percent = 0, StatusMessage = message });
                return ConversionResult.Failed(message);
            }

            if (!EnsureExecutablesPath())
            {
                string message = LocalizedStrings.Instance["FfmpegMissingConvert"];
                progress?.Report(new DownloadProgress { Percent = 0, StatusMessage = message });
                return ConversionResult.Failed(message);
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

                // ffmpeg не должен ждать ввода: про перезапись приложение спрашивает само,
                // а вопрос "Overwrite? [y/N]" на stdin намертво вешает конвертацию
                conversion.SetOverwriteOutput(true);
                conversion.AddParameter("-nostdin", ParameterPosition.PreInput);

                var parameters = BuildConversionParameters(options);
                foreach (var param in parameters)
                {
                    conversion.AddParameter(param);
                }

                cancellationToken.ThrowIfCancellationRequested();

                conversion.OnProgress += (sender, args) =>
                {
                    try
                    {
                        int percent;
                        string status;
                        if (args.TotalLength.TotalSeconds > 0)
                        {
                            percent = Math.Clamp(
                                (int)Math.Round(args.Duration.TotalSeconds / args.TotalLength.TotalSeconds * 100, 0),
                                0, 100);
                            status = $"[{args.Duration:hh\\:mm\\:ss} / {args.TotalLength:hh\\:mm\\:ss}] {percent}%";
                        }
                        else
                        {
                            // длительность неизвестна (некоторые TS/MKV) — показываем только позицию
                            percent = 0;
                            status = $"[{args.Duration:hh\\:mm\\:ss}]";
                        }

                        Debug.WriteLine(status);
                        progress?.Report(new DownloadProgress { Percent = percent, StatusMessage = status });
                    }
                    catch (Exception ex)
                    {
                        // ошибка расчёта прогресса не должна ронять конвертацию
                        Debug.WriteLine($"Progress error: {ex}");
                    }
                };

                await conversion.Start(cancellationToken).ConfigureAwait(false);

                return ConversionResult.Ok(options.OutputPath);
            }
            catch (OperationCanceledException)
            {
                progress?.Report(new DownloadProgress
                {
                    Percent = 0,
                    StatusMessage = LocalizedStrings.Instance["DownloadCancelled"]
                });
                return ConversionResult.CancelledResult();
            }
            catch (Exception ex)
            {
                // Xabe заворачивает stderr ffmpeg в исключение — это и есть причина отказа
                Debug.WriteLine($"FFmpeg error: {ex}");
                AppLog.Error("Converter", ex);
                return ConversionResult.Failed(ex.Message);
            }
        }

        public static List<string> BuildConversionParameters(ConversionOptions options)
        {
            var parameters = new List<string>();

            if (options.AudioOnly)
            {
                parameters.Add("-vn");
                parameters.Add($"-c:a {options.AudioCodec}");
                parameters.AddRange(ConversionOptions.GetSubtitleArgs(options.OutputFormat, audioOnly: true));

                if (options.AudioBitrate.HasValue && options.AudioBitrate.Value > 0
                    && ConversionOptions.SupportsAudioBitrate(options.AudioCodec))
                {
                    parameters.Add($"-b:a {options.AudioBitrate.Value}k");
                }

                return parameters;
            }

            parameters.Add($"-c:v {options.VideoCodec}");
            parameters.Add($"-c:a {options.AudioCodec}");
            parameters.AddRange(ConversionOptions.GetPresetArgs(options.VideoCodec, options.Preset));
            parameters.AddRange(ConversionOptions.GetQualityArgs(options.VideoCodec, options.Crf, options.VideoBitrate));
            parameters.AddRange(ConversionOptions.GetSubtitleArgs(options.OutputFormat, audioOnly: false));

            if (options.AudioBitrate.HasValue && options.AudioBitrate.Value > 0
                && ConversionOptions.SupportsAudioBitrate(options.AudioCodec))
            {
                parameters.Add($"-b:a {options.AudioBitrate.Value}k");
            }

            return parameters;
        }

        public static string BuildCommandPreview(ConversionOptions options)
        {
            var sb = new StringBuilder();
            sb.Append("ffmpeg -nostdin -i \"");
            sb.Append(options.InputPath);
            sb.Append("\" ");

            var parameters = BuildConversionParameters(options);
            foreach (var param in parameters)
            {
                sb.Append(param);
                sb.Append(' ');
            }

            sb.Append("-y \"");
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

        // ==== Проверка доступности аппаратных кодеров ====

        private static Dictionary<string, bool>? _hardwareEncoderProbeCache;

        private static readonly string[] ProbeCodecs =
        {
            "h264_nvenc", "hevc_nvenc", "av1_nvenc",
            "h264_amf", "hevc_amf",
            "h264_qsv", "hevc_qsv"
        };

        /// <summary>
        /// Проверяет аппаратные кодеры коротким тестовым кодированием (в null, без файлов).
        /// Наличие кодека в -encoders не гарантирует работающего GPU: nvenc может быть в сборке
        /// на машине без NVIDIA, а av1_nvenc — на карте без поддержки AV1.
        /// </summary>
        public static async Task<Dictionary<string, bool>> GetOrProbeHardwareEncodersAsync()
        {
            Dictionary<string, bool>? cached = _hardwareEncoderProbeCache;
            if (cached != null)
                return cached;

            var results = await Task.WhenAll(ProbeCodecs.Select(ProbeEncoderAsync)).ConfigureAwait(false);
            var map = new Dictionary<string, bool>();
            for (int i = 0; i < ProbeCodecs.Length; i++)
                map[ProbeCodecs[i]] = results[i];

            _hardwareEncoderProbeCache = map;
            return map;
        }

        private static async Task<bool> ProbeEncoderAsync(string codec)
        {
            string? ffmpegPath = FindFfmpegPath();
            if (string.IsNullOrEmpty(ffmpegPath) || !File.Exists(ffmpegPath))
                return false;

            try
            {
                var psi = new ProcessStartInfo(
                    ffmpegPath,
                    $"-hide_banner -v error -f lavfi -i testsrc=duration=0.3:size=256x256:rate=10 -c:v {codec} -f null NUL")
                {
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using var proc = Process.Start(psi);
                if (proc == null)
                    return false;

                // зависший драйвер не должен блокировать окно конвертера
                using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(20));
                try
                {
                    await proc.WaitForExitAsync(timeout.Token).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    try { proc.Kill(entireProcessTree: true); }
                    catch { }

                    return false;
                }

                return proc.ExitCode == 0;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Probe {codec} error: {ex.Message}");
                return false;
            }
        }

        public async Task<IMediaInfo?> GetMediaInfoAsync(string filePath)
        {
            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
            {
                return null;
            }

            if (!EnsureExecutablesPath())
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
