using System;
using System.IO;

namespace VidDownload.WPF
{
    public static class AppPaths
    {
        public static readonly string DataDir;
        public static readonly string ToolsDir;
        public static readonly string LogsDir;

        static AppPaths()
        {
            DataDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "VidDownload");
            ToolsDir = Path.Combine(DataDir, "tools");
            LogsDir = Path.Combine(DataDir, "logs");
            Directory.CreateDirectory(ToolsDir);
            Directory.CreateDirectory(LogsDir);
        }

        public static string ResolveToolPath(string fileName)
        {
            string dataPath = Path.Combine(ToolsDir, fileName);
            if (File.Exists(dataPath))
                return dataPath;

            string appPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, fileName);
            return File.Exists(appPath) ? appPath : dataPath;
        }
    }
}
