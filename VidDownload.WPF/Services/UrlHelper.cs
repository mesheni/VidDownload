using System;

namespace VidDownload.WPF.Services
{
    /// <summary>Проверка ссылок для поля URL и мониторинга буфера обмена.</summary>
    public static class UrlHelper
    {
        /// <summary>
        /// Похоже ли значение на http(s)-ссылку. yt-dlp также принимает поисковые
        /// запросы (ytsearch:...), поэтому они считаются допустимыми.
        /// </summary>
        public static bool LooksLikeVideoReference(string? text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return false;

            string value = text.Trim();

            if (value.StartsWith("ytsearch", StringComparison.OrdinalIgnoreCase))
                return true;

            if (value.Length > 2048)
                return false;

            if (!Uri.TryCreate(value, UriKind.Absolute, out var uri))
                return false;

            if (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
                return false;

            return uri.Host.Contains('.');
        }
    }
}
