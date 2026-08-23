using System;

namespace VidDownload.WPF.Services
{
    /// <summary>
    /// Посегментное сравнение версий вида "2025.08.23" или "0.7.1".
    /// В отличие от склеивания цифр в одно число корректно обрабатывает
    /// сегменты разной длины (2025.1.3 vs 2024.12.19) и суффиксы (2025.01.01-rc1).
    /// </summary>
    public static class VersionHelper
    {
        /// <summary>
        /// Сравнивает две точечные версии. Возвращает:
        /// отрицательное значение, если a &lt; b; 0, если равны; положительное, если a &gt; b.
        /// Нечисловые суффиксы сегментов игнорируются. Пустая или нечисловая версия считается наименьшей.
        /// </summary>
        public static int CompareDotted(string? a, string? b)
        {
            int[] pa = ParseSegments(a);
            int[] pb = ParseSegments(b);

            int count = Math.Max(pa.Length, pb.Length);
            for (int i = 0; i < count; i++)
            {
                int va = i < pa.Length ? pa[i] : 0;
                int vb = i < pb.Length ? pb[i] : 0;
                if (va != vb)
                    return va.CompareTo(vb);
            }
            return 0;
        }

        public static bool IsValidDotted(string? version)
        {
            return ParseSegments(version).Length > 0;
        }

        private static int[] ParseSegments(string? version)
        {
            if (string.IsNullOrWhiteSpace(version))
                return Array.Empty<int>();

            string cleaned = version.Trim().TrimStart('v', 'V');
            string[] parts = cleaned.Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (parts.Length == 0)
                return Array.Empty<int>();

            var segments = new int[parts.Length];
            for (int i = 0; i < parts.Length; i++)
            {
                // Берём ведущие цифры сегмента, суффикс вида "-rc1" игнорируем
                string segment = parts[i];
                int digitEnd = 0;
                while (digitEnd < segment.Length && char.IsDigit(segment[digitEnd]))
                    digitEnd++;

                if (digitEnd == 0 || !int.TryParse(segment[..digitEnd], out segments[i]))
                    return Array.Empty<int>();
            }
            return segments;
        }
    }
}
