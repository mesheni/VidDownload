using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace VidDownload.WPF.Control
{
    internal class ParseLog // Класс для парсинга лога yt-dlp
    {
        public static double Parse(string log)
        {
            string buff;
            double stringResult = 0;

            // Парсинг лога
            if (Regex.IsMatch(log, @"(\d{1}|\d{2})\.\d{1}"))
            {
                buff = Regex.Match(log, @"(\d{1}|\d{2})\.\d{1}").Value.ToString();
                buff = buff.Replace('.', ',');
                stringResult = Convert.ToDouble(buff);
                return stringResult;
            } else
            {
                return 0;
            }
        }
    }
}
