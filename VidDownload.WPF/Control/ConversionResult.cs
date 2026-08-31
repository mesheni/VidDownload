namespace VidDownload.WPF.Control
{
    /// <summary>
    /// Результат конвертации: путь к файлу при успехе, текст ошибки ffmpeg
    /// (сообщение исключения Xabe содержит stderr процесса) или признак отмены.
    /// </summary>
    internal class ConversionResult
    {
        public bool Success { get; private init; }

        public bool Cancelled { get; private init; }

        public string? OutputPath { get; private init; }

        public string? Error { get; private init; }

        public static ConversionResult Ok(string outputPath) =>
            new() { Success = true, OutputPath = outputPath };

        public static ConversionResult CancelledResult() =>
            new() { Cancelled = true };

        public static ConversionResult Failed(string error) =>
            new() { Error = error };
    }
}
