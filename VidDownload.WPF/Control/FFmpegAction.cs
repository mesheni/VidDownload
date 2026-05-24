namespace VidDownload.WPF.Control
{
    using System;
    using System.Diagnostics;
    using System.IO;
    using System.Threading.Tasks;
    using VidDownload.WPF.Services;
    using Xabe.FFmpeg;

    internal class FFmpegAction
    {
        public async Task<string?> ConvertVideoAsync(
            string inputPath,
            string outputPath,
            string outputFormat,
            bool useNVENC = false,
            IProgress<DownloadProgress>? progress = null)
        {
            if (string.IsNullOrEmpty(inputPath) || !File.Exists(inputPath))
            {
                progress?.Report(new DownloadProgress
                {
                    Percent = 0,
                    StatusMessage = $"Входной файл не найден: {inputPath}"
                });
                return null;
            }

            try
            {
                IMediaInfo mediaInfo = await FFmpeg.GetMediaInfo(inputPath).ConfigureAwait(false);

                var conversion = FFmpeg.Conversions.New();

                foreach (var stream in mediaInfo.Streams)
                {
                    conversion.AddStream(stream);
                }

                conversion.SetOutput(outputPath);

                switch (outputFormat.ToLower())
                {
                    case "mp4":
                        conversion.AddParameter("-c:v libx264");
                        conversion.AddParameter("-c:a aac");
                        if (useNVENC)
                        {
                            conversion.AddParameter("-c:v h264_nvenc");
                            conversion.AddParameter("-preset fast");
                        }
                        break;
                    case "avi":
                        conversion.AddParameter("-c:v mpeg4");
                        conversion.AddParameter("-c:a mp3");
                        break;
                    case "mkv":
                        conversion.AddParameter("-c:v libx264");
                        conversion.AddParameter("-c:a aac");
                        break;
                    case "mov":
                        conversion.AddParameter("-c:v libx264");
                        conversion.AddParameter("-c:a aac");
                        break;
                    default:
                        conversion.AddParameter("-c:v libx264");
                        conversion.AddParameter("-c:a aac");
                        break;
                }

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

                await conversion.Start().ConfigureAwait(false);

                return outputPath;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"FFmpeg error: {ex}");
                return null;
            }
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
    }
}
