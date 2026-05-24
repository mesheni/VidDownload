using System.Globalization;
using System.Text.RegularExpressions;
using VidDownload.WPF.Services;

namespace VidDownload.WPF.Control
{
    internal class ParseLog
    {
        private static readonly Regex ProgressRegex = new(
            @"\[download\]\s+(?<pct>[\d.]+)%\s+of\s+~?(?<size>[\d.]+)\s*(?<sizeUnit>[KMGTP]?i?B)\s+at\s+(?<spd>[\d.]+)\s*(?<spdUnit>[KMGTP]?i?B/s)\s+ETA\s+(?<eta>[\d:]+)",
            RegexOptions.Compiled);

        public static double Parse(string log)
        {
            var match = Regex.Match(log, @"\d{1,2}\.\d");
            if (match.Success)
            {
                return double.Parse(match.Value, CultureInfo.InvariantCulture);
            }

            return 0;
        }

        public static DownloadProgress ParseProgressLine(string log)
        {
            var match = ProgressRegex.Match(log);
            if (match.Success)
            {
                double percent = double.Parse(match.Groups["pct"].Value, CultureInfo.InvariantCulture);
                string totalSize = match.Groups["size"].Value + " " + match.Groups["sizeUnit"].Value;
                string speed = match.Groups["spd"].Value + " " + match.Groups["spdUnit"].Value;
                string eta = match.Groups["eta"].Value;

                return new DownloadProgress
                {
                    Percent = (int)percent,
                    StatusMessage = log,
                    Speed = speed,
                    Eta = eta,
                    TotalSize = totalSize
                };
            }

            return new DownloadProgress
            {
                Percent = 0,
                StatusMessage = log,
                Speed = "--",
                Eta = "--",
                TotalSize = "--"
            };
        }
    }
}
