namespace VidDownload.WPF.Control
{
    using System;
    using System.Diagnostics;
    using System.IO;
    using System.Threading.Tasks;
    using System.Windows;
    using Xabe.FFmpeg;

    /// <summary>
    /// Класс для выполнения операций FFmpeg с поддержкой прогресса и обработки ошибок
    /// </summary>
    internal class FFmpegAction
    {
        private readonly Action<int, string>? _onProgress;
        private readonly Action<string>? _onError;
        private readonly Action? _onCompleted;

        public FFmpegAction(Action<int, string>? onProgress = null, Action<string>? onError = null, Action? onCompleted = null)
        {
            _onProgress = onProgress;
            _onError = onError;
            _onCompleted = onCompleted;
        }

        /// <summary>
        /// Конвертировать видеофайл в указанный формат
        /// </summary>
        /// <param name="inputPath">Путь к входному файлу</param>
        /// <param name="outputPath">Путь к выходному файлу</param>
        /// <param name="outputFormat">Формат выходного файла (mp4, avi, mkv, mov)</param>
        /// <param name="useNVENC">Использовать ли NVENC для кодирования</param>
        /// <returns>Путь к сконвертированному файлу или null при ошибке</returns>
        public async Task<string?> ConvertVideoAsync(string inputPath, string outputPath, string outputFormat, bool useNVENC = false)
        {
            if (string.IsNullOrEmpty(inputPath) || !File.Exists(inputPath))
            {
                _onError?.Invoke($"Входной файл не найден: {inputPath}");
                return null;
            }

            try
            {
                // Получаем информацию о медиафайле
                IMediaInfo mediaInfo = await FFmpeg.GetMediaInfo(inputPath).ConfigureAwait(false);

                // Создаём новую конвертацию
                var conversion = FFmpeg.Conversions.New();

                // Добавляем потоки из исходного файла
                foreach (var stream in mediaInfo.Streams)
                {
                    conversion.AddStream(stream);
                }

                // Устанавливаем выходной путь
                conversion.SetOutput(outputPath);

                // Добавляем параметры кодирования в зависимости от формата
                switch (outputFormat.ToLower())
                {
                    case "mp4":
                        conversion.SetVideoCodec(VideoCodec.h264);
                        conversion.SetAudioCodec(AudioCodec.aac);
                        if (useNVENC)
                        {
                            conversion.AddParameter("-c:v h264_nvenc");
                            conversion.AddParameter("-preset fast");
                        }
                        break;
                    case "avi":
                        conversion.SetVideoCodec(VideoCodec.mpeg4);
                        conversion.SetAudioCodec(AudioCodec.mp3);
                        break;
                    case "mkv":
                        conversion.SetVideoCodec(VideoCodec.h264);
                        conversion.SetAudioCodec(AudioCodec.aac);
                        break;
                    case "mov":
                        conversion.SetVideoCodec(VideoCodec.h264);
                        conversion.SetAudioCodec(AudioCodec.aac);
                        break;
                    default:
                        conversion.SetVideoCodec(VideoCodec.h264);
                        conversion.SetAudioCodec(AudioCodec.aac);
                        break;
                }

                // Подписка на события прогресса
                conversion.OnProgress += (sender, args) =>
                {
                    int percent = (int)Math.Round(args.Duration.TotalSeconds / args.TotalLength.TotalSeconds * 100, 0);
                    Debug.WriteLine($"[{args.Duration} / {args.TotalLength}] {percent}%");
                    _onProgress?.Invoke(percent, $"[{args.Duration} / {args.TotalLength}] {percent}%");
                };

                // Запуск конвертации
                await conversion.Start().ConfigureAwait(false);

                _onCompleted?.Invoke();
                return outputPath;
            }
            catch (Exception ex)
            {
                _onError?.Invoke($"Ошибка конвертации: {ex.Message}");
                Debug.WriteLine($"FFmpeg error: {ex}");
                return null;
            }
        }

        /// <summary>
        /// Получить информацию о медиафайле
        /// </summary>
        /// <param name="filePath">Путь к файлу</param>
        /// <returns>Информация о медиа или null при ошибке</returns>
        public async Task<IMediaInfo?> GetMediaInfoAsync(string filePath)
        {
            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
            {
                _onError?.Invoke($"Файл не найден: {filePath}");
                return null;
            }

            try
            {
                return await FFmpeg.GetMediaInfo(filePath).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _onError?.Invoke($"Ошибка получения информации: {ex.Message}");
                Debug.WriteLine($"FFmpeg info error: {ex}");
                return null;
            }
        }
    }
}
