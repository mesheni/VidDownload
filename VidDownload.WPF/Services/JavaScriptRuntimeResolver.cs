using System;
using System.IO;

namespace VidDownload.WPF.Services
{
    /// <summary>Находит JavaScript runtime, необходимый yt-dlp для YouTube.</summary>
    public static class JavaScriptRuntimeResolver
    {
        public static string? ResolveRuntime()
        {
            return SelectRuntime(
                FindExecutable("deno.exe"),
                FindExecutable("node.exe"),
                File.Exists);
        }

        public static string? SelectRuntime(string? denoPath, string? nodePath, Func<string, bool> fileExists)
        {
            if (!string.IsNullOrWhiteSpace(denoPath) && fileExists(denoPath))
                return $"deno:{denoPath}";

            if (!string.IsNullOrWhiteSpace(nodePath) && fileExists(nodePath))
                return $"node:{nodePath}";

            return null;
        }

        private static string? FindExecutable(string executableName)
        {
            string[] preferredDirectories =
            {
                AppPaths.ToolsDir,
                AppDomain.CurrentDomain.BaseDirectory
            };

            foreach (string directory in preferredDirectories)
            {
                string candidate = Path.Combine(directory, executableName);
                if (File.Exists(candidate))
                    return candidate;
            }

            string? path = Environment.GetEnvironmentVariable("PATH");
            if (string.IsNullOrWhiteSpace(path))
                return null;

            foreach (string directory in path.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries))
            {
                try
                {
                    string candidate = Path.Combine(directory.Trim(), executableName);
                    if (File.Exists(candidate))
                        return candidate;
                }
                catch (ArgumentException)
                {
                    // Некорректная запись PATH не должна ломать загрузку.
                }
            }

            return null;
        }
    }
}
