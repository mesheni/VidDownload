using System;
using System.IO;

namespace VidDownload.WPF.Services
{
    /// <summary>
    /// Простой файловый логгер для диагностики ошибок служб
    /// (настройки, история загрузок, обновления). Не бросает исключений.
    /// </summary>
    public static class AppLog
    {
        private static readonly object LockObj = new();

        public static void Error(string source, string message)
        {
            Write("ERROR", source, message);
        }

        public static void Error(string source, Exception exception)
        {
            Write("ERROR", source, $"{exception.GetType().Name}: {exception.Message}");
        }

        public static void Info(string source, string message)
        {
            Write("INFO", source, message);
        }

        private static void Write(string level, string source, string message)
        {
            try
            {
                string line = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} [{level}] [{source}] {message}{Environment.NewLine}";
                lock (LockObj)
                {
                    File.AppendAllText(Path.Combine(AppPaths.LogsDir, "app.log"), line);
                }
            }
            catch
            {
                // Логирование не должно ломать приложение
            }
        }
    }
}
