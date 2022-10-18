using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace VidDownload.Download
{
    internal class ParseLog
    {
        public string Parse(string log)
        {
            string resultString;

            if (Regex.IsMatch(log, @"(\d{1}|\d{2})\.\d{1}"))
            {
                resultString = Regex.Match(log, @"(\d{1}|\d{2})\.\d{1}").Value.ToString();
                resultString = resultString.Split('.')[0];
                return resultString;
            } else
            {
                return "0";
            }
        }
    }
}
