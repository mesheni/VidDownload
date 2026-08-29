using System;
using System.Globalization;

namespace VidDownload.WPF.Control
{
    /// <summary>Разбор таймкодов фрагмента («90», «1:30», «01:02:03») для --download-sections.</summary>
    public static class Timecodes
    {
        /// <summary>Парсит «ss», «mm:ss» или «hh:mm:ss» в секунды.</summary>
        public static bool TryParse(string input, out double seconds)
        {
            seconds = 0;
            if (string.IsNullOrWhiteSpace(input))
                return false;

            var parts = input.Trim().Split(':');
            if (parts.Length > 3)
                return false;

            double[] values = new double[parts.Length];
            for (int i = 0; i < parts.Length; i++)
            {
                if (!double.TryParse(parts[i].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out values[i]) ||
                    values[i] < 0)
                    return false;
            }

            seconds = values[^1];                 // секунды
            if (values.Length > 1) seconds += values[^2] * 60;      // минуты
            if (values.Length > 2) seconds += values[^3] * 3600;    // часы
            return true;
        }

        public static string Format(TimeSpan time) => time.ToString(@"hh\:mm\:ss", CultureInfo.InvariantCulture);

        /// <summary>Строит «*start-end» для yt-dlp; обе границы обязательны, начало строго меньше конца.</summary>
        public static bool TryBuildSection(string startInput, string endInput, out string section)
        {
            section = string.Empty;
            if (!TryParse(startInput, out double start) || !TryParse(endInput, out double end))
                return false;
            if (start >= end)
                return false;

            section = "*" + Format(TimeSpan.FromSeconds(start)) + "-" + Format(TimeSpan.FromSeconds(end));
            return true;
        }
    }
}
