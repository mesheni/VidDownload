using System.Collections.ObjectModel;
using System.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using VidDownload.WPF.Control;
using VidDownload.WPF.ConvertWindow;
using VidDownload.WPF.Help;
using VidDownload.WPF.HistoryWindow;
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
        private readonly IFFmpegService _ffmpegService;
        private readonly ISettingsService _settingsService;
        private readonly IMessageService _messageService;
        private readonly IDialogService _dialogService;
        private readonly IDownloadHistoryService _historyService;
        private CancellationTokenSource? _cts;
        private bool _wasCancelled;

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
        [NotifyCanExecuteChangedFor(nameof(CancelCommand))]
        private bool _isDownloading;

        [ObservableProperty]
        private bool _isVideoOptionsVisible = true;

        [ObservableProperty]
        private bool _isAudioOptionsVisible;

        [ObservableProperty]
        private string _linkLabelText = "Поле для ссылки на видео:";

        [ObservableProperty]
        private string _speedText = "--";

        [ObservableProperty]
        private string _etaText = "--";

        [ObservableProperty]
        private string _totalSizeText = "--";

        [ObservableProperty]
        private string _ffmpegVersion = "проверка...";

        [ObservableProperty]
        private bool _isFfmpegChecking = true;

        [ObservableProperty]
        private bool _isFfmpegUpdateAvailable;

        [ObservableProperty]
        private string _ffmpegStatusMessage = string.Empty;

        [ObservableProperty]
        private bool _isDownloadSubtitles;

        [ObservableProperty]
        private string _selectedSubtitleLanguage = "all";

        [ObservableProperty]
        private bool _isEmbedSubtitles;

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

        public ObservableCollection<string> SubtitleLanguages { get; } = new()
        {
            "", "all", "en", "ru", "de", "fr", "es", "ja", "zh-Hans", "ar", "pt"
        };

        public MainViewModel(IYtDlpService ytDlpService, IUpdateService updateService, IFFmpegService ffmpegService, ISettingsService settingsService, IMessageService messageService, IDialogService dialogService, IDownloadHistoryService historyService)
        {
            _ytDlpService = ytDlpService;
            _updateService = updateService;
            _ffmpegService = ffmpegService;
            _settingsService = settingsService;
            _messageService = messageService;
            _dialogService = dialogService;
            _historyService = historyService;
            foreach (var item in Codecs)
            {
                _codecList.Add(item);
            }
            _ = CheckUpdateAsync();
            _ = CheckFFmpegUpdateAsync();
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

            _settings.DownloadSubtitles = IsDownloadSubtitles;
            _settings.SubtitleLanguage = SelectedSubtitleLanguage;
            _settings.EmbedSubtitles = IsEmbedSubtitles;

            if (IsEmbedSubtitles && IsAudioOnly)
            {
                _messageService.Warning("Встраивание субтитров недоступно для аудио.", "Предупреждение");
                IsEmbedSubtitles = false;
                _settings.EmbedSubtitles = false;
            }
            else if (IsEmbedSubtitles && SelectedFormat == "avi")
            {
                if (!await _dialogService.AskAsync(
                    "Формат AVI может не поддерживать встроенные субтитры. Продолжить?",
                    "Предупреждение"))
                {
                    return;
                }
            }

            string downloadUrl = Url;

            _cts?.Dispose();
            _cts = new CancellationTokenSource();
            _wasCancelled = false;

            IsDownloading = true;
            StatusMessage = string.Empty;
            ProgressPercent = 0;

            try
            {
                var progress = new Progress<DownloadProgress>(p =>
                {
                    StatusMessage = p.StatusMessage;
                    ProgressPercent = p.Percent;
                    SpeedText = p.Speed;
                    EtaText = p.Eta;
                    TotalSizeText = p.TotalSize;
                });

                await _ytDlpService.DownloadAsync(downloadUrl, _settings, IsPlaylist, IsAudioOnly, IsReEncode, progress, _cts.Token).ConfigureAwait(true);

                await _historyService.AddEntryAsync(new DownloadHistoryEntry
                {
                    Url = downloadUrl,
                    Title = downloadUrl,
                    Timestamp = DateTime.Now,
                    Status = DownloadStatus.Completed
                });
            }
            catch (OperationCanceledException)
            {
                _wasCancelled = true;
            }
            catch (Exception ex)
            {
                await _historyService.AddEntryAsync(new DownloadHistoryEntry
                {
                    Url = downloadUrl,
                    Title = downloadUrl,
                    Timestamp = DateTime.Now,
                    Status = DownloadStatus.Failed
                });
                _messageService.Error($"Ошибка: {ex.Message}", "Ошибка");
            }
            finally
            {
                _cts?.Dispose();
                _cts = null;

                await SaveSettingsAsync();
                ProgressPercent = 0;
                SelectedCodec = "";
                SelectedResolution = "";
                SelectedAudioFormat = "";
                SelectedFormat = "";
                Url = "";
                IsDownloading = false;
                StatusMessage = _wasCancelled ? "Загрузка отменена" : "";
                SpeedText = "--";
                EtaText = "--";
                TotalSizeText = "--";
            }
        }

        private bool CanCancel() => IsDownloading;

        [RelayCommand(CanExecute = nameof(CanCancel))]
        private async Task CancelAsync()
        {
            if (_cts == null || _cts.IsCancellationRequested)
                return;

            bool confirmed = await _dialogService.ConfirmAsync(
                "Вы уверены, что хотите отменить загрузку?", "Подтверждение отмены");

            if (!confirmed)
                return;

            _cts.Cancel();
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

        [RelayCommand]
        private void OpenHistory()
        {
            var history = AppServices.ServiceProvider.GetRequiredService<HistoryWindow.HistoryWindow>();
            if (history.ShowDialog() == true && !string.IsNullOrEmpty(history.SelectedUrl))
            {
                Url = history.SelectedUrl;
            }
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
            IsDownloadSubtitles = userSettings.DownloadSubtitles;
            if (!string.IsNullOrEmpty(userSettings.SubtitleLanguage))
                SelectedSubtitleLanguage = userSettings.SubtitleLanguage;
            IsEmbedSubtitles = userSettings.EmbedSubtitles;
        }

        private async Task SaveSettingsAsync()
        {
            await _settingsService.SaveAsync(new UserSettings
            {
                Resolution = _settings.Resolution,
                VideoCodec = _settings.VideoCodec,
                AudioCodec = _settings.AudioCodec,
                Format = _settings.Format,
                DownloadSubtitles = _settings.DownloadSubtitles,
                SubtitleLanguage = _settings.SubtitleLanguage,
                EmbedSubtitles = _settings.EmbedSubtitles
            });
        }

        [RelayCommand]
        private async Task CheckFFmpegUpdateAsync()
        {
            if (IsFfmpegChecking)
                return;

            IsFfmpegChecking = true;
            IsFfmpegUpdateAvailable = false;
            FfmpegStatusMessage = "Поиск обновлений FFmpeg...";

            try
            {
                var info = await _ffmpegService.CheckForUpdateAsync();

                string localVer = await _ffmpegService.GetLocalVersionAsync();
                FfmpegVersion = string.IsNullOrEmpty(localVer) ? "не установлен" : localVer;

                if (info.IsUpdateAvailable)
                {
                    IsFfmpegUpdateAvailable = true;
                    FfmpegStatusMessage = $"Доступна версия {info.LatestVersion}";

                    if (await _dialogService.AskAsync(
                        $"Текущая версия: {(string.IsNullOrEmpty(localVer) ? "не найдена" : localVer)}\n" +
                        $"Последняя версия: {info.LatestVersion}\nЗагрузить и установить?",
                        "Доступно обновление FFmpeg!"))
                    {
                        IsFfmpegChecking = true;
                        FfmpegVersion = "обновление...";

                        var progress = new Progress<DownloadProgress>(p =>
                        {
                            if (!string.IsNullOrEmpty(p.StatusMessage))
                                FfmpegStatusMessage = p.StatusMessage;
                        });

                        await _ffmpegService.DownloadUpdateAsync(info, progress);

                        string newVer = await _ffmpegService.GetLocalVersionAsync();
                        FfmpegVersion = string.IsNullOrEmpty(newVer) ? "установлен" : newVer;
                        IsFfmpegUpdateAvailable = false;
                        FfmpegStatusMessage = "FFmpeg успешно обновлён";
                    }
                }
                else if (!string.IsNullOrEmpty(localVer))
                {
                    FfmpegStatusMessage = "FFmpeg актуален";
                }
                else
                {
                    FfmpegStatusMessage = "FFmpeg не найден";
                }
            }
            catch
            {
                FfmpegStatusMessage = "Ошибка проверки обновления FFmpeg";
            }
            finally
            {
                IsFfmpegChecking = false;
            }
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
                !await _dialogService.AskAsync(
                    $"Текущая версия: {displayCurrent}\nПоследняя версия: {info.Version}\nПодтвердите начало обновления.",
                    "Доступна новая версия yt-dlp!"))
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

                _messageService.Info(
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
