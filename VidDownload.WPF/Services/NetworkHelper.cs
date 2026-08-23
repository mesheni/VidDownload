using System;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using VidDownload.WPF.Resources;

namespace VidDownload.WPF.Services
{
    /// <summary>
    /// Общий сетевой функционал: единственный HttpClient на приложение,
    /// проверка доступа к интернету и безопасное скачивание файлов
    /// (проверка HTTP-статуса, запись через временный файл).
    /// </summary>
    public static class NetworkHelper
    {
        /// <summary>Единый HttpClient для всех загрузок приложения.</summary>
        public static readonly HttpClient Http = CreateHttpClient();

        private static HttpClient CreateHttpClient()
        {
            var client = new HttpClient(new SocketsHttpHandler
            {
                PooledConnectionLifetime = TimeSpan.FromMinutes(10)
            })
            {
                Timeout = TimeSpan.FromMinutes(30)
            };
            client.DefaultRequestHeaders.UserAgent.ParseAdd("VidDownload");
            return client;
        }

        /// <summary>
        /// Проверяет доступность интернета (github.com, затем google.com).
        /// Заменяет дублировавшиеся реализации на устаревшем HttpWebRequest.
        /// </summary>
        public static async Task<bool> IsInternetAvailableAsync(int timeoutMs = 5000)
        {
            return await ProbeAsync("https://github.com", timeoutMs).ConfigureAwait(false)
                || await ProbeAsync("https://www.google.com", timeoutMs).ConfigureAwait(false);
        }

        private static async Task<bool> ProbeAsync(string url, int timeoutMs)
        {
            try
            {
                using var cts = new CancellationTokenSource(timeoutMs);
                using var request = new HttpRequestMessage(HttpMethod.Head, url);
                using var response = await Http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cts.Token).ConfigureAwait(false);
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Скачивает файл по URL в <paramref name="destPath"/> с проверкой
        /// HTTP-статуса и записью через временный файл: при ошибке или обрыве
        /// соединения существующий файл не повреждается.
        /// </summary>
        public static async Task DownloadFileAsync(
            string url,
            string destPath,
            IProgress<DownloadProgress>? progress,
            string fileLabel,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(url))
                throw new InvalidOperationException("Download URL is empty");

            string? dir = Path.GetDirectoryName(destPath);
            if (!string.IsNullOrEmpty(dir))
                Directory.CreateDirectory(dir);

            string tempPath = destPath + ".tmp";
            try
            {
                using var response = await Http.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);
                response.EnsureSuccessStatusCode();

                long totalBytes = response.Content.Headers.ContentLength ?? -1;
                await using var contentStream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);

                // Поток записи закрывается ДО File.Move: на Windows нельзя переместить
                // файл с открытым хэндлом (IOException "file is being used by another process")
                using (var fileStream = new FileStream(tempPath, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    var buffer = new byte[81920];
                    long totalRead = 0;
                    int bytesRead;
                    while ((bytesRead = await contentStream.ReadAsync(buffer, cancellationToken).ConfigureAwait(false)) > 0)
                    {
                        await fileStream.WriteAsync(buffer.AsMemory(0, bytesRead), cancellationToken).ConfigureAwait(false);
                        totalRead += bytesRead;
                        if (totalBytes > 0 && progress != null)
                        {
                            int percent = (int)(totalRead * 100 / totalBytes);
                            progress.Report(new DownloadProgress
                            {
                                Percent = percent,
                                StatusMessage = string.Format(LocalizedStrings.Instance["DownloadingProgress"], fileLabel, percent)
                            });
                        }
                    }

                    await fileStream.FlushAsync(cancellationToken).ConfigureAwait(false);
                }

                File.Move(tempPath, destPath, overwrite: true);
            }
            finally
            {
                try
                {
                    if (File.Exists(tempPath))
                        File.Delete(tempPath);
                }
                catch
                {
                    // Не критично
                }
            }
        }
    }
}
