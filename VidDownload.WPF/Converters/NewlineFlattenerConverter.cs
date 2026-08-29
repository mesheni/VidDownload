using System;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Windows.Data;

namespace VidDownload.WPF.Converters
{
    /// <summary>
    /// Схлопывает переводы строк в разделитель « · », чтобы многострочные ошибки
    /// yt-dlp не растягивали карточку очереди. Полный текст остаётся в подсказке.
    /// </summary>
    public class NewlineFlattenerConverter : IValueConverter
    {
        private static readonly Regex Newlines = new(@"(\r?\n)+", RegexOptions.Compiled);

        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is not string text || string.IsNullOrEmpty(text))
                return string.Empty;

            var flat = Newlines.Replace(text, "  ·  ").Trim();
            const int maxChars = 220;
            if (flat.Length > maxChars)
                flat = flat[..maxChars] + "…";
            return flat;
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }
}
