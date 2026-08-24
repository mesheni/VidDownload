using System.Globalization;
using System.Text.RegularExpressions;
using VidDownload.WPF.Services;

namespace VidDownload.WPF.Control
{
    internal class ParseLog
    {
        // Полная строка прогресса: процент и размер, скорость и ETA опциональны
        // (могут быть "Unknown"), между размером и скоростью может стоять "in HH:MM:SS".
        // [download]  42.3% of ~10.00MiB at 2.50MiB/s ETA 00:12:34
        // [download] 100% of 10.00MiB in 00:00:10 at 2.50MiB/s
        // [download]  42.3% of 10.00MiB at Unknown B/s ETA Unknown
        private static readonly Regex ProgressRegex = new(
            @"\[download\]\s+(?<pct>[\d.]+)%\s+of\s+~?(?<size>[\d.]+)\s*(?<sizeUnit>[KMGTP]?i?B)" +
            @"(?:\s+in\s+[\d:]+)?" +
            @"(?:\s+at\s+(?:(?<spd>[\d.]+)\s*(?<spdUnit>[KMGTP]?i?B/s)|Unknown\s*\S*/s))?" +
            @"(?:\s+ETA\s+(?:(?<eta>[\d:]+)|Unknown))?",
            RegexOptions.Compiled);

        // Строка без процента (размер итогового файла неизвестен):
        // [download]   1.50MiB at 2.00MiB/s ETA 00:00:05
        private static readonly Regex NoPercentRegex = new(
            @"\[download\]\s+(?<size>[\d.]+)\s*(?<sizeUnit>[KMGTP]?i?B)\s+at\s+" +
            @"(?:(?<spd>[\d.]+)\s*(?<spdUnit>[KMGTP]?i?B/s)|Unknown\s*\S*/s)" +
            @"(?:\s+ETA\s+(?:(?<eta>[\d:]+)|Unknown))?",
            RegexOptions.Compiled);

        // [download] Destination: C:\path\file.mp4
        private static readonly Regex DestinationRegex = new(
            @"^\[download\]\s+Destination:\s+(?<path>.+)$",
            RegexOptions.Compiled);

        // [Merger] Merging formats into "C:\path\file.mp4"
        private static readonly Regex MergerRegex = new(
            @"^\[Merger\]\s+Merging formats into\s+""(?<path>.+)""",
            RegexOptions.Compiled);

        // Начало нового видео плейлиста. yt-dlp печатает "item" или "video"
        // в зависимости от экстрактора:
        // [download] Downloading item 3 of 15
        // [download] Downloading video 3 of 15
        private static readonly Regex PlaylistItemRegex = new(
            @"^\[download\]\s+Downloading\s+(?:item|video)\s+(?<n>\d+)\s+of\s+(?<m>\d+)",
            RegexOptions.Compiled);

        // Название плейлиста:
        // [download] Downloading playlist: My favorite mix
        private static readonly Regex PlaylistTitleRegex = new(
            @"^\[download\]\s+Downloading\s+playlist:\s*(?<title>.+)$",
            RegexOptions.Compiled);

        public static DownloadProgress ParseProgressLine(string log, DownloadProgress? previous = null)
        {
            var result = new DownloadProgress
            {
                Percent = previous?.Percent ?? 0,
                Speed = previous?.Speed ?? "--",
                Eta = previous?.Eta ?? "--",
                TotalSize = previous?.TotalSize ?? "--",
                PlaylistIndex = previous?.PlaylistIndex,
                PlaylistCount = previous?.PlaylistCount,
                PlaylistTitle = previous?.PlaylistTitle,
                StatusMessage = log
            };

            var destination = DestinationRegex.Match(log);
            if (destination.Success)
            {
                result.DestinationPath = destination.Groups["path"].Value.Trim();
                return result;
            }

            var merger = MergerRegex.Match(log);
            if (merger.Success)
            {
                result.DestinationPath = merger.Groups["path"].Value.Trim();
                return result;
            }

            var playlistItem = PlaylistItemRegex.Match(log);
            if (playlistItem.Success)
            {
                // Началось новое видео плейлиста: прогресс предыдущего уже неактуален.
                result.PlaylistIndex = int.Parse(playlistItem.Groups["n"].Value, CultureInfo.InvariantCulture);
                result.PlaylistCount = int.Parse(playlistItem.Groups["m"].Value, CultureInfo.InvariantCulture);
                result.Percent = 0;
                result.Speed = "--";
                result.Eta = "--";
                result.TotalSize = "--";
                return result;
            }

            var playlistTitle = PlaylistTitleRegex.Match(log);
            if (playlistTitle.Success)
            {
                result.PlaylistTitle = playlistTitle.Groups["title"].Value.Trim();
                return result;
            }

            var match = ProgressRegex.Match(log);
            if (!match.Success)
                match = NoPercentRegex.Match(log);

            if (match.Success)
            {
                if (match.Groups["pct"].Success)
                    result.Percent = (int)Math.Round(double.Parse(match.Groups["pct"].Value, CultureInfo.InvariantCulture), MidpointRounding.AwayFromZero);

                if (match.Groups["size"].Success)
                    result.TotalSize = match.Groups["size"].Value + " " + match.Groups["sizeUnit"].Value;

                if (match.Groups["spd"].Success)
                    result.Speed = match.Groups["spd"].Value + " " + match.Groups["spdUnit"].Value;

                if (match.Groups["eta"].Success)
                    result.Eta = match.Groups["eta"].Value;
            }

            return result;
        }
    }
}
