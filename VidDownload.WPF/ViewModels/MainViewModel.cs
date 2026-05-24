using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using VidDownload.WPF.Control;
using VidDownload.WPF.ConvertWindow;
using VidDownload.WPF.Help;
using VidDownload.WPF.Services;
using VidDownload.WPF.ViewModels.Base;

namespace VidDownload.WPF.ViewModels
{
    public partial class MainViewModel : ViewModelBase
    {
        private readonly Settings _settings = new();
        private static readonly List<string> _codecList = new();
        private readonly IYtDlpService _ytDlpService;
        private readonly IUpdateService _updateService;
        private readonly ISettingsService _settingsService;

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

        public MainViewModel(IYtDlpService ytDlpService, IUpdateService updateService, ISettingsService settingsService)
        {
            _ytDlpService = ytDlpService;
            _updateService = updateService;
            _settingsService = settingsService;
            foreach (var item in Codecs)
            {
                _codecList.Add(item);
            }
            _ = CheckUpdateAsync();
            _ = LoadSettingsAsync();
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
                var progress = new Progress<DownloadProgress>(p =>
                {
                    StatusMessage = p.StatusMessage;
                    ProgressPercent = p.Percent;
                });

                await _ytDlpService.DownloadAsync(Url, _settings, IsPlaylist, IsAudioOnly, IsReEncode, progress, CancellationToken.None).ConfigureAwait(true);
            }
            catch (Exception ex)
            {
                HandyControl.Controls.MessageBox.Error($"Ошибка: {ex.Message}", "Ошибка");
            }
            finally
            {
                await SaveSettingsAsync();
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
            var convert = AppServices.ServiceProvider.GetRequiredService<ConvertWindow.ConvertWindow>();
            convert.ShowDialog();
        }

        [RelayCommand]
        private void OpenHelp()
        {
            var help = AppServices.ServiceProvider.GetRequiredService<HelpWindow>();
            help.ShowDialog();
        }

        private async Task LoadSettingsAsync()
        {
            var userSettings = await _settingsService.LoadAsync();
            if (!string.IsNullOrEmpty(userSettings.Resolution))
                SelectedResolution = userSettings.Resolution;
            if (!string.IsNullOrEmpty(userSettings.VideoCodec))
                SelectedCodec = userSettings.VideoCodec;
            if (!string.IsNullOrEmpty(userSettings.AudioCodec))
                SelectedAudioFormat = userSettings.AudioCodec;
            if (!string.IsNullOrEmpty(userSettings.Format))
                SelectedFormat = userSettings.Format;
            if (!string.IsNullOrEmpty(userSettings.LastUrl))
                Url = userSettings.LastUrl;
        }

        private async Task SaveSettingsAsync()
        {
            await _settingsService.SaveAsync(new UserSettings
            {
                Resolution = _settings.Resolution,
                VideoCodec = _settings.VideoCodec,
                AudioCodec = _settings.AudioCodec,
                Format = _settings.Format,
                LastUrl = Url
            });
        }

        public async Task CheckUpdateAsync()
        {
            UpdateInfo info;

            try
            {
                info = await _updateService.CheckForUpdateAsync();
            }
            catch
            {
                return;
            }

            if (!info.IsUpdateAvailable)
                return;

            string currentVer = await _updateService.GetCurrentVersionAsync();
            bool fileNotFound = string.IsNullOrEmpty(currentVer);
            string displayCurrent = fileNotFound ? "не найдена" : currentVer;

            if (!fileNotFound &&
                HandyControl.Controls.MessageBox.Ask(
                    $"Текущая версия: {displayCurrent}\nПоследняя версия: {info.Version}\nПодтвердите начало обновления.",
                    "Доступна новая версия yt-dlp!") != MessageBoxResult.OK)
            {
                return;
            }

            IsDownloading = true;
            StatusMessage = "Идет загрузка обновления yt-dlp!";

            try
            {
                var progress = new Progress<DownloadProgress>(p =>
                {
                    ProgressPercent = p.Percent;
                    StatusMessage = p.StatusMessage;
                });

                await _updateService.DownloadUpdateAsync(info, progress);

                HandyControl.Controls.MessageBox.Info(
                    $"Версия yt-dlp обновлена до {info.Version}",
                    "Обновление завершено!");
            }
            finally
            {
                ProgressPercent = 0;
                StatusMessage = "";
                IsDownloading = false;
            }
        }
    }
}
