using System;
using System.Text.RegularExpressions;


namespace VidDownload.WPF.Control
{

    /// <summary>
    /// Анализирует строку лога для извлечения процентного значения.
    /// Ищет шаблон, подобный "12.3", и преобразует его в double. 
    /// Возвращает 0, если совпадение не найдено.
    /// </summary>
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
            }
            else
            {
                return 0;
            }
        }
    }
}
