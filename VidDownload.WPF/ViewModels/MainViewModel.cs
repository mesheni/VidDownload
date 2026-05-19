using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Octokit;
using VidDownload.WPF.Control;
using VidDownload.WPF.ConvertWindow;
using VidDownload.WPF.Help;
using VidDownload.WPF.ViewModels.Base;

namespace VidDownload.WPF.ViewModels
{
    public partial class MainViewModel : ViewModelBase
    {
        private readonly Settings _settings = new();
        private static readonly List<string> _codecList = new();

        [ObservableProperty]
        private string _url = string.Empty;

        [ObservableProperty]
        private string _selectedResolution = string.Empty;

        [ObservableProperty]
        private string _selectedCodec = string.Empty;

        [ObservableProperty]
        private string _selectedAudioFormat = string.Empty;

        [ObservableProperty]
        private string _selectedFormat = string.Empty;

        [ObservableProperty]
        private bool _isPlaylist;

        [ObservableProperty]
        private bool _isAudioOnly;

        [ObservableProperty]
        private bool _isReEncode;

        [ObservableProperty]
        private string _statusMessage = string.Empty;

        [ObservableProperty]
        private int _progressPercent;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(DownloadCommand))]
        private bool _isDownloading;

        [ObservableProperty]
        private bool _isVideoOptionsVisible = true;

        [ObservableProperty]
        private bool _isAudioOptionsVisible;

        [ObservableProperty]
        private string _linkLabelText = "Поле для ссылки на видео:";

        public ObservableCollection<string> Resolutions { get; } = new()
        {
            "", "144", "240", "360", "480", "720", "1080", "1440", "2160"
        };

        public ObservableCollection<string> Codecs { get; } = new()
        {
            "", "av01", "vp9.2", "vp9", "h265", "h264", "vp8", "h263"
        };

        public ObservableCollection<string> AudioFormats { get; } = new()
        {
            "", "mp3", "3gp", "flac", "wav", "aac", "m4a"
        };

        public ObservableCollection<string> Formats { get; } = new()
        {
            "", "avi", "mkv", "mp4", "webm"
        };

        public MainViewModel()
        {
            foreach (var item in Codecs)
            {
                _codecList.Add(item);
            }
            _ = CheckUpdateAsync();
        }

        partial void OnIsAudioOnlyChanged(bool value)
        {
            IsVideoOptionsVisible = !value;
            IsAudioOptionsVisible = value;
        }

        partial void OnIsPlaylistChanged(bool value)
        {
            LinkLabelText = value ? "Поле для ссылки на плейлист:" : "Поле для ссылки на видео:";
        }

        partial void OnIsReEncodeChanged(bool value)
        {
            if (value)
            {
                if (!Formats.Contains("mov"))
                    Formats.Add("mov");
            }
            else
            {
                Formats.Remove("mov");
            }
        }

        private bool CanDownload() => !IsDownloading;

        [RelayCommand]
        private async Task DownloadAsync()
        {
            if (string.IsNullOrWhiteSpace(Url))
            {
                StatusMessage = "Пустое поле ссылки!";
                return;
            }

            if (SelectedResolution.Length != 0)
                _settings.Resolution = SelectedResolution;
            if (SelectedAudioFormat.Length != 0)
                _settings.AudioCodec = SelectedAudioFormat;
            if (SelectedFormat.Length != 0)
                _settings.Format = SelectedFormat;
            if (SelectedCodec.Length != 0 && _codecList.Exists(i => i == SelectedCodec))
                _settings.VideoCodec = SelectedCodec;

            IsDownloading = true;
            StatusMessage = string.Empty;
            ProgressPercent = 0;

            try
            {
                string dateTime = DateTime.Now.ToString("yyyy-MM-dd HH_mm_ss");
                string log = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory + @"log\" + dateTime + "_log.txt");
                string logDir = System.IO.Path.GetDirectoryName(log);

                if (!string.IsNullOrEmpty(logDir) && !System.IO.Directory.Exists(logDir))
                {
                    System.IO.Directory.CreateDirectory(logDir);
                }

                string args;
                if (IsAudioOnly)
                {
                    args = Command.LoadAudio(_settings, Url, IsPlaylist);
                }
                else
                {
                    args = Command.LoadVideo(Url, _settings, IsPlaylist, IsReEncode);
                }

                await Task.Run(() =>
                {
                    Process proc = new();

                    proc.StartInfo.FileName = @".\yt-dlp.exe";
                    proc.StartInfo.UseShellExecute = false;
                    proc.StartInfo.RedirectStandardOutput = true;
                    proc.StartInfo.CreateNoWindow = true;
                    proc.StartInfo.Arguments = args;

                    try
                    {
                        using System.IO.FileStream fs = new(log, System.IO.FileMode.CreateNew);
                        using System.IO.StreamWriter w = new(fs, Encoding.Default);

                        proc.OutputDataReceived += new DataReceivedEventHandler((sender, e) =>
                        {
                            if (!string.IsNullOrEmpty(e.Data))
                            {
                                w.WriteLine(e.Data);
                                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                                {
                                    StatusMessage = e.Data;
                                    ProgressPercent = (int)ParseLog.Parse(e.Data);
                                });
                            }
                        });

                        proc.Start();
                        proc.BeginOutputReadLine();
                        proc.WaitForExit();

                        if (proc.ExitCode != 0)
                        {
                            System.Windows.Application.Current.Dispatcher.Invoke(() =>
                            {
                                HandyControl.Controls.MessageBox.Error($"yt-dlp завершился с ошибкой (код: {proc.ExitCode}). Проверьте логи.", "Ошибка загрузки");
                            });
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Windows.Application.Current.Dispatcher.Invoke(() =>
                        {
                            HandyControl.Controls.MessageBox.Error($"Ошибка при выполнении yt-dlp: {ex.Message}", "Ошибка");
                        });
                    }
                    finally
                    {
                        proc.Close();
                    }
                }).ConfigureAwait(true);
            }
            catch (Exception ex)
            {
                HandyControl.Controls.MessageBox.Error($"Ошибка: {ex.Message}", "Ошибка");
            }
            finally
            {
                ProgressPercent = 0;
                SelectedCodec = "";
                SelectedResolution = "";
                SelectedAudioFormat = "";
                SelectedFormat = "";
                Url = "";
                IsDownloading = false;
                StatusMessage = "";
            }
        }

        [RelayCommand]
        private void OpenFolder()
        {
            string path = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory + @"MyVideos\");
            if (!System.IO.Directory.Exists(path))
            {
                System.IO.Directory.CreateDirectory(path);
            }
            Process.Start("explorer.exe", "/open, \"" + path);
        }

        [RelayCommand]
        private void OpenConverter()
        {
            ConvertWindow.ConvertWindow convert = new();
            convert.ShowDialog();
        }

        [RelayCommand]
        private void OpenHelp()
        {
            HelpWindow help = new();
            help.ShowDialog();
        }

        public async Task CheckUpdateAsync()
        {
            if (await CheckForInternetConnection())
                await Task.Run(async () =>
                {
                    bool fileNotFound = false;
                    string? links = "";
                    string currentVer = string.Empty;
                    MessageBoxResult res = new();

                    var client = new GitHubClient(new Octokit.ProductHeaderValue("VidDownload"));
                    var latest = await client.Repository.Release.GetLatest("yt-dlp", "yt-dlp");
                    string latestVer = latest.TagName.Replace(".", "");

                    try
                    {
                        var versionInfo = FileVersionInfo.GetVersionInfo("yt-dlp.exe");
                        currentVer = versionInfo.FileVersion.Replace(".", "");

                        if (Convert.ToInt32(currentVer) < Convert.ToInt32(latestVer))
                        {
                            res = HandyControl.Controls.MessageBox.Ask($"Текущая версия: {currentVer} \nПоследняя версия: {latestVer}\nПодтвердите начало обновления.", "Доступна новая версия yt-dlp!");
                        }
                    }
                    catch
                    {
                        HandyControl.Controls.MessageBox.Error("При проверке обновлений не был найден файл yt-dlp!\nБудет загружена последняя версия.", "Ошибка!");
                        fileNotFound = true;
                    }

                    if (res == MessageBoxResult.OK || fileNotFound)
                    {
                        foreach (var release in latest.Assets)
                        {
                            if (release.BrowserDownloadUrl.Contains("yt-dlp.exe"))
                            {
                                links = release.BrowserDownloadUrl;
                            }
                        }

                        System.Windows.Application.Current.Dispatcher.Invoke(() =>
                        {
                            IsDownloading = true;
                            StatusMessage = "Идет загрузка обновления yt-dlp!";
                        });

                        using var httpClient = new HttpClient();
                        httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("MyUserAgent");

                        using var response = await httpClient.GetAsync(links, HttpCompletionOption.ResponseHeadersRead);
                        var totalBytes = response.Content.Headers.ContentLength ?? -1;
                        using var contentStream = await response.Content.ReadAsStreamAsync();
                        using var fileStream = new System.IO.FileStream("yt-dlp.exe", System.IO.FileMode.Create, System.IO.FileAccess.Write);

                        var buffer = new byte[8192];
                        long totalRead = 0;
                        int bytesRead;
                        while ((bytesRead = await contentStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                        {
                            await fileStream.WriteAsync(buffer, 0, bytesRead);
                            totalRead += bytesRead;
                            if (totalBytes > 0)
                            {
                                System.Windows.Application.Current.Dispatcher.Invoke(() => ProgressPercent = (int)(totalRead * 100 / totalBytes));
                            }
                        }

                        System.Windows.Application.Current.Dispatcher.Invoke(() =>
                        {
                            ProgressPercent = 0;
                            StatusMessage = "";
                            IsDownloading = false;
                        });
                        HandyControl.Controls.MessageBox.Info($"Версия yt-dlp обновлена до {latest.TagName}", "Обновление завершено!");
                    }
                }).ConfigureAwait(false);
        }

        private async Task<bool> CheckForInternetConnection(int timeoutMs = 1000, string url = null)
        {
            bool result = false;
            await Task.Run(() =>
            {
                try
                {
                    url ??= CultureInfo.InstalledUICulture switch
                    {
                        { Name: var n } when n.StartsWith("ru") => "https://ya.ru/",
                        _ => "http://www.gstatic.com/generate_204",
                    };

                    var request = (HttpWebRequest)WebRequest.Create(url);
                    request.KeepAlive = false;
                    request.Timeout = timeoutMs;
                    using (var response = (HttpWebResponse)request.GetResponse())
                        result = true;
                }
                catch
                {
                    result = false;
                }
            }).ConfigureAwait(false);

            return result;
        }
    }
}
