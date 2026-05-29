using System.Collections.ObjectModel;
using System.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using VidDownload.WPF.Control;
using VidDownload.WPF.ConvertWindow;
using VidDownload.WPF.Help;
using VidDownload.WPF.HistoryWindow;
using VidDownload.WPF.Resources;
using VidDownload.WPF.Services;
using VidDownload.WPF.ViewModels.Base;

namespace VidDownload.WPF.ViewModels
{
    public partial class MainViewModel : ViewModelBase
    {
        private readonly Settings _settings = new();
        private static readonly List<string> _codecList = new();
        private readonly LocalizedStrings _loc;
        private readonly IYtDlpService _ytDlpService;
        private readonly IUpdateService _updateService;
        private readonly IFFmpegService _ffmpegService;
        private readonly ISettingsService _settingsService;
        private readonly IMessageService _messageService;
        private readonly IDialogService _dialogService;
        private readonly IDownloadHistoryService _historyService;
        private CancellationTokenSource? _cts;
        private bool _wasCancelled;
        private bool _isLoading;
        private string _savePath = UserSettings.DefaultDownloadPath;

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
        private string _linkLabelText = LocalizedStrings.Instance["LinkLabelVideo"];

        [ObservableProperty]
        private string _selectedLanguage = "RU";

        [ObservableProperty]
        private string _speedText = "--";

        [ObservableProperty]
        private string _etaText = "--";

        [ObservableProperty]
        private string _totalSizeText = "--";

        [ObservableProperty]
        private string _ffmpegVersion = LocalizedStrings.Instance["FFmpegChecking"];

        [ObservableProperty]
        private bool _isFfmpegChecking;

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

        public LocalizedStrings LocalizedStrings => _loc;

        public ObservableCollection<string> AvailableLanguages { get; } = new()
        {
            "RU", "EN", "ZH"
        };

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

        public MainViewModel(IYtDlpService ytDlpService, IUpdateService updateService, IFFmpegService ffmpegService, ISettingsService settingsService, IMessageService messageService, IDialogService dialogService, IDownloadHistoryService historyService, LocalizedStrings localizedStrings)
        {
            _loc = localizedStrings;
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
            LinkLabelText = value ? _loc["LinkLabelPlaylist"] : _loc["LinkLabelVideo"];
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

        partial void OnSelectedLanguageChanged(string value)
        {
            if (_isLoading)
                return;
            if (_loc.CurrentLanguage == value.ToUpper())
                return;
            _loc.SetLanguage(value.ToLower());
            _ = SaveSettingsAsync();
        }

        private bool CanDownload() => !IsDownloading;

        [RelayCommand]
        private async Task DownloadAsync()
        {
            if (string.IsNullOrWhiteSpace(Url))
            {
                StatusMessage = _loc["EmptyLink"];
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
            _settings.SavePath = _savePath;

            if (IsEmbedSubtitles && IsAudioOnly)
            {
                _messageService.Warning(_loc["SubtitleEmbedNotForAudio"], _loc["WarningTitle"]);
                IsEmbedSubtitles = false;
                _settings.EmbedSubtitles = false;
            }
            else if (IsEmbedSubtitles && SelectedFormat == "avi")
            {
                if (!await _dialogService.AskAsync(
                    _loc["AviSubtitleWarning"],
                    _loc["WarningTitle"]))
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
                if (!System.IO.Directory.Exists(_savePath))
                    System.IO.Directory.CreateDirectory(_savePath);
            }
            catch (UnauthorizedAccessException)
            {
                _messageService.Warning(_loc["NoSaveFolderAccess"], _loc["ErrorTitle"]);
                IsDownloading = false;
                return;
            }

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
                _messageService.Error(string.Format(_loc["ErrorWithMessage"], ex.Message), _loc["ErrorTitle"]);
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
                StatusMessage = _wasCancelled ? _loc["DownloadCancelled"] : "";
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
                _loc["ConfirmCancelDownload"], _loc["CancelConfirmTitle"]);

            if (!confirmed)
                return;

            _cts.Cancel();
        }

        [RelayCommand]
        private void OpenFolder()
        {
            if (!System.IO.Directory.Exists(_savePath))
            {
                System.IO.Directory.CreateDirectory(_savePath);
            }
            Process.Start("explorer.exe", "/open, \"" + _savePath);
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
            _isLoading = true;
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
            if (!string.IsNullOrEmpty(userSettings.Language))
            {
                SelectedLanguage = userSettings.Language;
                _loc.SetLanguage(userSettings.Language.ToLower());
            }
            _savePath = !string.IsNullOrEmpty(userSettings.SavePath)
                ? userSettings.SavePath
                : UserSettings.DefaultDownloadPath;
            try
            {
                if (!System.IO.Directory.Exists(_savePath))
                    System.IO.Directory.CreateDirectory(_savePath);
            }
            catch (UnauthorizedAccessException)
            {
                _savePath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "VidDownload");
                System.IO.Directory.CreateDirectory(_savePath);
                _messageService.Warning(
                    _loc["NoVideoFolderAccess"],
                    _loc["WarningTitle"]);
            }
            _isLoading = false;
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
                EmbedSubtitles = _settings.EmbedSubtitles,
                SavePath = _savePath,
                Language = SelectedLanguage
            });
        }

        [RelayCommand]
        private async Task CheckFFmpegUpdateAsync()
        {
            if (IsFfmpegChecking)
                return;

            IsFfmpegChecking = true;
            IsFfmpegUpdateAvailable = false;
            FfmpegStatusMessage = _loc["CheckingFFmpeg"];

            try
            {
                var info = await _ffmpegService.CheckForUpdateAsync();

                string localVer = await _ffmpegService.GetLocalVersionAsync();
                FfmpegVersion = string.IsNullOrEmpty(localVer) ? _loc["FFmpegNotInstalled"] : localVer;

                if (!info.IsUpdateAvailable)
                {
                    if (!string.IsNullOrEmpty(localVer))
                        FfmpegStatusMessage = _loc["FFmpegUpToDate"];
                    else
                        FfmpegStatusMessage = _loc["FFmpegNotFound"];
                    return;
                }

                if (string.IsNullOrEmpty(info.DownloadUrl))
                {
                    _messageService.Error(
                        _loc["FFmpegDownloadLinkError"],
                        _loc["UpdateErrorTitle"]);
                    FfmpegStatusMessage = _loc["FFmpegLinkNotFound"];
                    return;
                }

                IsFfmpegUpdateAvailable = true;
                string displayLatest = info.LatestVersion.Length > 30
                    ? info.LatestVersion[..27] + "..."
                    : info.LatestVersion;
                FfmpegStatusMessage = string.Format(_loc["FFmpegVersionAvailable"], displayLatest);

                string displayCurrent = string.IsNullOrEmpty(localVer) ? _loc["VersionNotFound"] : localVer;
                if (!await _dialogService.AskAsync(
                    string.Format(_loc["FFmpegUpdateDialog"], displayCurrent, displayLatest),
                    _loc["FFmpegUpdateAvailableTitle"]))
                {
                    return;
                }

                IsFfmpegChecking = true;
                FfmpegVersion = _loc["FFmpegUpdating"];

                var progress = new Progress<DownloadProgress>(p =>
                {
                    if (!string.IsNullOrEmpty(p.StatusMessage))
                        FfmpegStatusMessage = p.StatusMessage;
                });

                await _ffmpegService.DownloadUpdateAsync(info, progress);

                string newVer = await _ffmpegService.GetLocalVersionAsync();
                FfmpegVersion = string.IsNullOrEmpty(newVer) ? _loc["FFmpegInstalled"] : newVer;
                IsFfmpegUpdateAvailable = false;
                FfmpegStatusMessage = _loc["FFmpegUpdated"];

                _messageService.Info(
                    string.Format(_loc["FFmpegUpdateInfoMessage"], displayLatest, newVer),
                    _loc["FFmpegUpdateInfoTitle"]);
            }
            catch (Exception ex)
            {
                FfmpegStatusMessage = string.Format(_loc["ErrorWithMessage"], ex.Message);
                _messageService.Error(
                    string.Format(_loc["FFmpegUpdateFailed"], ex.Message),
                    _loc["UpdateErrorTitle"]);
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
            string displayCurrent = fileNotFound ? _loc["VersionNotFound"] : currentVer;

            if (!fileNotFound &&
                !await _dialogService.AskAsync(
                    string.Format(_loc["YtDlpUpdateDialog"], displayCurrent, info.Version),
                    _loc["YtDlpUpdateAvailableTitle"]))
            {
                return;
            }

            IsDownloading = true;
            StatusMessage = _loc["YtDlpDownloading"];

            try
            {
                var progress = new Progress<DownloadProgress>(p =>
                {
                    ProgressPercent = p.Percent;
                    StatusMessage = p.StatusMessage;
                });

                await _updateService.DownloadUpdateAsync(info, progress);

                _messageService.Info(
                    string.Format(_loc["YtDlpUpdated"], info.Version),
                    _loc["UpdateCompletedTitle"]);
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
