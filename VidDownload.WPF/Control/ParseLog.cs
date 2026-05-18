using System.Globalization;
using System.Text.RegularExpressions;

namespace VidDownload.WPF.Control
{
    internal class ParseLog
    {
        public static double Parse(string log)
        {
            var match = Regex.Match(log, @"\d{1,2}\.\d");
            if (match.Success)
            {
                return double.Parse(match.Value, CultureInfo.InvariantCulture);
            }

            return 0;
        }
    }
}
