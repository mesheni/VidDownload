using System.Diagnostics;

namespace VidDownload.Updater;

internal static class Program
{
    private static int Main(string[] args)
    {
        string? src = null;
        string? dst = null;
        int pid = -1;

        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i].ToLowerInvariant())
            {
                case "--src" when i + 1 < args.Length:
                    src = args[++i];
                    break;
                case "--dst" when i + 1 < args.Length:
                    dst = args[++i];
                    break;
                case "--pid" when i + 1 < args.Length:
                    int.TryParse(args[++i], out pid);
                    break;
            }
        }

        if (string.IsNullOrEmpty(src) || string.IsNullOrEmpty(dst) || pid == -1)
        {
            Console.Error.WriteLine("Usage: Updater.exe --src <temp\\new.exe> --dst <app\\VidDownload.WPF.exe> --pid <current-pid>");
            return 1;
        }

        if (!File.Exists(src))
        {
            Console.Error.WriteLine($"Source file not found: {src}");
            return 2;
        }

        // Защита от битых загрузок: исходник должен быть PE-исполняемым файлом (сигнатура MZ),
        // иначе можно затереть рабочее приложение мусором (HTML-страницей ошибки и т.п.)
        if (!IsPortableExecutable(src))
        {
            Console.Error.WriteLine($"Source file is not a valid executable: {src}");
            return 5;
        }

        try
        {
            var targetProcess = Process.GetProcessById(pid);
            Console.WriteLine($"Waiting for process {pid} ({targetProcess.ProcessName}) to exit...");
            targetProcess.WaitForExit();
        }
        catch (ArgumentException)
        {
            // Process already exited
        }

        int maxRetries = 10;
        for (int retry = 1; retry <= maxRetries; retry++)
        {
            try
            {
                File.Copy(src, dst, overwrite: true);
                Console.WriteLine($"Replaced: {dst}");
                break;
            }
            catch (IOException) when (retry < maxRetries)
            {
                Console.WriteLine($"File locked, retrying in 1s ({retry}/{maxRetries})...");
                Thread.Sleep(1000);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Failed to replace file: {ex.Message}");
                return 3;
            }
        }

        // Больше не нужен: скачанный обновлённый exe лежал во временной папке
        try { File.Delete(src); }
        catch { /* не критично */ }

        try
        {
            var startInfo = new ProcessStartInfo(dst)
            {
                WorkingDirectory = Path.GetDirectoryName(dst),
                UseShellExecute = true
            };
            Process.Start(startInfo);
            Console.WriteLine($"Restarted: {dst}");
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Failed to restart: {ex.Message}");
            return 4;
        }

        return 0;
    }

    private static bool IsPortableExecutable(string path)
    {
        try
        {
            using var stream = File.OpenRead(path);
            if (stream.Length < 2)
                return false;
            return stream.ReadByte() == 'M' && stream.ReadByte() == 'Z';
        }
        catch
        {
            return false;
        }
    }
}
