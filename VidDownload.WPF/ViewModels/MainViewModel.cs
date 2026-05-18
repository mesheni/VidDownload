using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
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

                if (!string.IsNullOrEmpty(logDir) && !Directory.Exists(logDir))
                {
                    Directory.CreateDirectory(logDir);
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
                        using FileStream fs = new(log, System.IO.FileMode.CreateNew);
                        using StreamWriter w = new(fs, Encoding.Default);

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
                                StatusMessage = $"yt-dlp завершился с ошибкой (код: {proc.ExitCode})";
                            });
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Windows.Application.Current.Dispatcher.Invoke(() =>
                        {
                            StatusMessage = $"Ошибка при выполнении yt-dlp: {ex.Message}";
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
                StatusMessage = $"Ошибка: {ex.Message}";
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
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
            Process.Start("explorer.exe", "/open, \"" + path);
        }

        [RelayCommand]
        private void OpenConverter()
        {
            ConvertWindow convert = new();
            convert.ShowDialog();
        }

        [RelayCommand]
        private void OpenHelp()
        {
            HelpWindow help = new();
            help.ShowDialog();
        }
    }
}
