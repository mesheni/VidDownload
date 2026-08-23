using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Win32;
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
        private readonly LocalizedStrings _loc;
        private readonly IUpdateService _updateService;
        private readonly IFFmpegService _ffmpegService;
        private readonly ISettingsService _settingsService;
        private readonly IMessageService _messageService;
        private readonly IDialogService _dialogService;
        private readonly IDownloadHistoryService _historyService;
        private readonly IDownloadQueueService _queue;
        private readonly INotificationService _notifications;
        private readonly IClipboardMonitorService _clipboardMonitor;
        private DownloadItem? _activeItem;
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

        [ObservableProperty]
        private string _appVersion = string.Empty;

        [ObservableProperty]
        private bool _isAppUpdateChecking;

        [ObservableProperty]
        private bool _isAppUpdateAvailable;

        [ObservableProperty]
        private string _appUpdateStatusMessage = string.Empty;

        [ObservableProperty]
        private string _savePathText = string.Empty;

        [ObservableProperty]
        private string _rateLimit = string.Empty;

        [ObservableProperty]
        private bool _minimizeToTray;

        [ObservableProperty]
        private bool _isClipboardMonitorEnabled;

        [ObservableProperty]
        private bool _hasActiveDownloads;

        [ObservableProperty]
        private bool _hasQueueItems;

        private AppUpdateInfo? _appUpdateInfo;

        public LocalizedStrings LocalizedStrings => _loc;

        public IDownloadQueueService Queue => _queue;

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

        public MainViewModel(
            IUpdateService updateService,
            IFFmpegService ffmpegService,
            ISettingsService settingsService,
            IMessageService messageService,
            IDialogService dialogService,
            IDownloadHistoryService historyService,
            IDownloadQueueService queue,
            INotificationService notifications,
            IClipboardMonitorService clipboardMonitor,
            LocalizedStrings localizedStrings)
        {
            _loc = localizedStrings;
            _updateService = updateService;
            _ffmpegService = ffmpegService;
            _settingsService = settingsService;
            _messageService = messageService;
            _dialogService = dialogService;
            _historyService = historyService;
            _queue = queue;
            _notifications = notifications;
            _clipboardMonitor = clipboardMonitor;

            _queue.ItemStarted += OnItemStarted;
            _queue.ItemCompleted += OnItemCompleted;
            _queue.ItemFailed += OnItemFailed;
            _queue.ItemCancelled += OnItemCancelled;
            _queue.Items.CollectionChanged += (_, _) => HasQueueItems = _queue.Items.Count > 0;

            _clipboardMonitor.UrlDetected += OnClipboardUrlDetected;

            RunSafe(CheckUpdateAsync, nameof(CheckUpdateAsync));
            RunSafe(CheckFFmpegUpdateAsync, nameof(CheckFFmpegUpdateAsync));
            RunSafe(CheckAppUpdateAsync, nameof(CheckAppUpdateAsync));
            RunSafe(LoadSettingsAsync, nameof(LoadSettingsAsync));
            AppVersion = GetAppVersion();
        }

        private async void RunSafe(Func<Task> action, string operationName)
        {
            try
            {
                await action();
            }
            catch (Exception ex)
            {
                AppLog.Error(nameof(MainViewModel), $"{operationName} failed: {ex}");
            }
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
            RunSafe(SaveSettingsAsync, nameof(SaveSettingsAsync));
        }

        partial void OnSavePathTextChanged(string value)
        {
            if (_isLoading)
                return;
            _savePath = value;
            RunSafe(SaveSettingsAsync, nameof(SaveSettingsAsync));
        }

        partial void OnRateLimitChanged(string value)
        {
            if (_isLoading)
                return;
            _settings.RateLimit = value?.Trim() ?? string.Empty;
            RunSafe(SaveSettingsAsync, nameof(SaveSettingsAsync));
        }

        partial void OnMinimizeToTrayChanged(bool value)
        {
            if (_isLoading)
                return;
            RunSafe(SaveSettingsAsync, nameof(SaveSettingsAsync));
        }

        partial void OnIsClipboardMonitorEnabledChanged(bool value)
        {
            if (_isLoading)
                return;
            _clipboardMonitor.IsEnabled = value;
            RunSafe(SaveSettingsAsync, nameof(SaveSettingsAsync));
        }

        // ==== Очередь загрузок ====

        [RelayCommand]
        private async Task DownloadAsync()
        {
            if (string.IsNullOrWhiteSpace(Url))
            {
                StatusMessage = _loc["EmptyLink"];
                return;
            }

            if (!UrlHelper.LooksLikeVideoReference(Url) &&
                !await _dialogService.AskAsync(_loc["InvalidUrlWarning"], _loc["WarningTitle"]))
            {
                return;
            }

            if (!await ValidateOptionsAsync())
                return;

            Enqueue(Url);
            Url = string.Empty;
        }

        /// <summary>Добавляет ссылку в очередь с текущими настройками (без интерактивных проверок).</summary>
        private void Enqueue(string url, bool notify = false)
        {
            ApplySelectionsToSettings();

            var item = new DownloadItem(url, _settings.Clone(), IsPlaylist, IsAudioOnly, IsReEncode);
            _queue.Enqueue(item);

            if (notify)
                _notifications.Info(string.Format(_loc["AddedToQueue"], url));

            RunSafe(SaveSettingsAsync, nameof(SaveSettingsAsync));
        }

        /// <summary>Переносит текущие выбранные параметры интерфейса в настройки загрузки.</summary>
        private void ApplySelectionsToSettings()
        {
            if (SelectedResolution.Length != 0)
                _settings.Resolution = SelectedResolution;
            if (SelectedAudioFormat.Length != 0)
                _settings.AudioCodec = SelectedAudioFormat;
            if (SelectedFormat.Length != 0)
                _settings.Format = SelectedFormat;
            if (SelectedCodec.Length != 0 && Codecs.Contains(SelectedCodec))
                _settings.VideoCodec = SelectedCodec;

            _settings.DownloadSubtitles = IsDownloadSubtitles;
            _settings.SubtitleLanguage = SelectedSubtitleLanguage;
            _settings.EmbedSubtitles = IsEmbedSubtitles;
            _settings.SavePath = _savePath;
            _settings.RateLimit = RateLimit?.Trim() ?? string.Empty;
        }

        private async Task<bool> ValidateOptionsAsync()
        {
            if (IsEmbedSubtitles && IsAudioOnly)
            {
                _messageService.Warning(_loc["SubtitleEmbedNotForAudio"], _loc["WarningTitle"]);
                IsEmbedSubtitles = false;
            }
            else if (IsEmbedSubtitles && SelectedFormat == "avi")
            {
                if (!await _dialogService.AskAsync(_loc["AviSubtitleWarning"], _loc["WarningTitle"]))
                    return false;
            }
            return true;
        }

        [RelayCommand]
        private void PauseResumeItem(DownloadItem? item)
        {
            if (item == null)
                return;
            if (item.Status == DownloadItemStatus.Paused)
                _queue.Resume(item);
            else if (item.Status is DownloadItemStatus.Downloading or DownloadItemStatus.Queued)
                _queue.Pause(item);
        }

        [RelayCommand]
        private async Task CancelItemAsync(DownloadItem? item)
        {
            if (item == null)
                return;
            if (item.Status == DownloadItemStatus.Downloading &&
                !await _dialogService.AskAsync(_loc["ConfirmCancelDownload"], _loc["CancelConfirmTitle"]))
            {
                return;
            }
            _queue.Cancel(item);
        }

        [RelayCommand]
        private void RemoveItem(DownloadItem? item)
        {
            if (item != null)
                _queue.Remove(item);
        }

        [RelayCommand]
        private void ClearFinished()
        {
            _queue.ClearFinished();
        }

        /// <summary>Esc: отменяет текущую активную загрузку (с подтверждением).</summary>
        [RelayCommand]
        private async Task CancelActiveAsync()
        {
            var active = _queue.Items.FirstOrDefault(i => i.Status == DownloadItemStatus.Downloading)
                ?? _queue.Items.FirstOrDefault(i => i.Status == DownloadItemStatus.Queued);
            if (active == null)
                return;
            await CancelItemAsync(active);
        }

        private void OnItemStarted(object? sender, DownloadItem item)
        {
            SetActiveItem(item);
            RefreshActivity();
        }

        private void OnItemCompleted(object? sender, DownloadItem item)
        {
            RefreshActivity();
            ResetSummaryIfIdle();
            _notifications.Success(string.Format(_loc["DownloadCompleteNotify"], item.DisplayTitle));
            RunSafe(() => AddHistoryAsync(item, DownloadStatus.Completed), nameof(AddHistoryAsync));
        }

        private void OnItemFailed(object? sender, DownloadItem item)
        {
            RefreshActivity();
            ResetSummaryIfIdle();
            _notifications.Error(string.Format(_loc["DownloadFailedNotify"], item.DisplayTitle));
            RunSafe(() => AddHistoryAsync(item, DownloadStatus.Failed), nameof(AddHistoryAsync));
        }

        private void OnItemCancelled(object? sender, DownloadItem item)
        {
            RefreshActivity();
            ResetSummaryIfIdle();
            // Не записываем в историю элементы, которые ни разу не запускались
            if (item.Started)
                RunSafe(() => AddHistoryAsync(item, DownloadStatus.Cancelled), nameof(AddHistoryAsync));
        }

        /// <summary>
        /// Когда все загрузки завершены, нижний индикатор возвращается в исходное
        /// состояние — иначе на нём навсегда остаются 100% и скорость последней загрузки.
        /// </summary>
        private void ResetSummaryIfIdle()
        {
            if (_queue.HasActiveDownloads)
                return;
            ProgressPercent = 0;
            StatusMessage = string.Empty;
            SpeedText = "--";
            EtaText = "--";
            TotalSizeText = "--";
        }

        private async Task AddHistoryAsync(DownloadItem item, DownloadStatus status)
        {
            await _historyService.AddEntryAsync(new DownloadHistoryEntry
            {
                Url = item.Url,
                Title = item.DisplayTitle,
                FilePath = item.FilePath,
                Timestamp = DateTime.Now,
                Status = status
            });
        }

        private void RefreshActivity()
        {
            HasActiveDownloads = _queue.HasActiveDownloads;
        }

        private void SetActiveItem(DownloadItem? item)
        {
            if (_activeItem != null)
                _activeItem.PropertyChanged -= OnActiveItemPropertyChanged;

            _activeItem = item;

            if (_activeItem != null)
            {
                _activeItem.PropertyChanged += OnActiveItemPropertyChanged;
                CopyItemToSummary(_activeItem);
            }
        }

        private void OnActiveItemPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (_activeItem == null)
                return;
            if (e.PropertyName is null or nameof(DownloadItem.StatusMessage) or nameof(DownloadItem.Percent)
                or nameof(DownloadItem.Speed) or nameof(DownloadItem.Eta) or nameof(DownloadItem.TotalSize))
            {
                CopyItemToSummary(_activeItem);
            }
        }

        private void CopyItemToSummary(DownloadItem item)
        {
            StatusMessage = item.StatusMessage;
            ProgressPercent = item.Percent;
            SpeedText = item.Speed;
            EtaText = item.Eta;
            TotalSizeText = item.TotalSize;
        }

        // ==== Папка сохранения и прочие команды ====

        [RelayCommand]
        private void BrowseFolder()
        {
            var dialog = new OpenFolderDialog
            {
                Title = _loc["OutputFolderDialogTitle"],
                InitialDirectory = Directory.Exists(_savePath)
                    ? _savePath
                    : Environment.GetFolderPath(Environment.SpecialFolder.MyVideos)
            };

            if (dialog.ShowDialog() == true)
            {
                SavePathText = dialog.FolderName;
            }
        }

        [RelayCommand]
        private void OpenFolder()
        {
            if (!Directory.Exists(_savePath))
            {
                Directory.CreateDirectory(_savePath);
            }
            Process.Start("explorer.exe", $"\"{_savePath}\"");
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

        // ==== Буфер обмена ====

        private void OnClipboardUrlDetected(object? sender, string url)
        {
            if (Url == url)
                return;

            _notifications.Ask(
                string.Format(_loc["ClipboardLinkDetected"], url),
                _loc["ClipboardLinkTitle"],
                () => Enqueue(url, notify: true));
        }

        // ==== Настройки ====

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
            SavePathText = _savePath;
            RateLimit = userSettings.RateLimit ?? string.Empty;
            _settings.RateLimit = RateLimit;
            MinimizeToTray = userSettings.MinimizeToTray;
            IsClipboardMonitorEnabled = userSettings.ClipboardMonitorEnabled;
            _queue.MaxConcurrent = Math.Clamp(userSettings.MaxConcurrentDownloads <= 0 ? 1 : userSettings.MaxConcurrentDownloads, 1, 3);

            try
            {
                if (!Directory.Exists(_savePath))
                    Directory.CreateDirectory(_savePath);
            }
            catch (UnauthorizedAccessException)
            {
                _savePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyVideos), "VidDownload");
                Directory.CreateDirectory(_savePath);
                SavePathText = _savePath;
                _messageService.Warning(_loc["NoVideoFolderAccess"], _loc["WarningTitle"]);
            }
            _isLoading = false;
        }

        private async Task SaveSettingsAsync()
        {
            ApplySelectionsToSettings();
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
                Language = SelectedLanguage,
                RateLimit = RateLimit ?? string.Empty,
                MaxConcurrentDownloads = _queue.MaxConcurrent,
                MinimizeToTray = MinimizeToTray,
                ClipboardMonitorEnabled = IsClipboardMonitorEnabled
            });
        }

        // ==== Обновление FFmpeg ====

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
                    _messageService.Error(_loc["FFmpegDownloadLinkError"], _loc["UpdateErrorTitle"]);
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

                _notifications.Success(string.Format(_loc["FFmpegUpdateInfoMessage"], displayLatest, newVer));
            }
            catch (Exception ex)
            {
                FfmpegStatusMessage = string.Format(_loc["ErrorWithMessage"], ex.Message);
                _messageService.Error(string.Format(_loc["FFmpegUpdateFailed"], ex.Message), _loc["UpdateErrorTitle"]);
            }
            finally
            {
                IsFfmpegChecking = false;
            }
        }

        // ==== Обновление yt-dlp ====

        public async Task CheckUpdateAsync()
        {
            UpdateInfo info;

            try
            {
                info = await _updateService.CheckForUpdateAsync();
            }
            catch (Exception ex)
            {
                AppLog.Error(nameof(MainViewModel), $"yt-dlp update check failed: {ex.Message}");
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

            try
            {
                var progress = new Progress<DownloadProgress>(p =>
                {
                    // Общий индикатор занят только когда нет активных загрузок
                    if (_activeItem == null)
                    {
                        ProgressPercent = p.Percent;
                        StatusMessage = p.StatusMessage;
                    }
                });

                await _updateService.DownloadUpdateAsync(info, progress);

                _notifications.Success(string.Format(_loc["YtDlpUpdated"], info.Version), _loc["UpdateCompletedTitle"]);
            }
            catch (Exception ex)
            {
                _notifications.Error(string.Format(_loc["ErrorWithMessage"], ex.Message), _loc["UpdateErrorTitle"]);
            }
            finally
            {
                if (_activeItem == null)
                {
                    ProgressPercent = 0;
                    StatusMessage = string.Empty;
                }
            }
        }

        // ==== Обновление приложения ====

        public async Task CheckAppUpdateAsync()
        {
            if (IsAppUpdateChecking)
                return;

            IsAppUpdateChecking = true;
            AppUpdateStatusMessage = _loc["AppCheckingUpdate"];

            try
            {
                var info = await _updateService.CheckAppUpdateAsync();
                _appUpdateInfo = info;

                if (!info.IsUpdateAvailable)
                {
                    AppUpdateStatusMessage = _loc["AppUpToDate"];
                    return;
                }

                IsAppUpdateAvailable = true;
                string displayLatest = info.Version.Length > 20
                    ? info.Version[..17] + "..."
                    : info.Version;
                AppUpdateStatusMessage = string.Format(_loc["AppUpdateAvailable"], displayLatest);
            }
            catch
            {
                AppUpdateStatusMessage = _loc["AppUpdateCheckError"];
            }
            finally
            {
                IsAppUpdateChecking = false;
            }
        }

        [RelayCommand]
        private async Task UpdateAppAsync()
        {
            if (_appUpdateInfo == null || !_appUpdateInfo.IsUpdateAvailable)
                return;

            // В релизе нет portable .exe — предложить открыть страницу загрузки
            if (string.IsNullOrEmpty(_appUpdateInfo.DownloadUrl))
            {
                AppUpdateStatusMessage = _loc["AppUpdateManualOnly"];
                if (await _dialogService.AskAsync(_loc["AppUpdateOpenPage"], _loc["AppUpdateDownloadTitle"]))
                {
                    Process.Start(new ProcessStartInfo(UpdateService.ReleasesUrl) { UseShellExecute = true });
                }
                return;
            }

            string currentVer = string.IsNullOrEmpty(AppVersion) ? _loc["VersionNotFound"] : AppVersion;
            string displayLatest = _appUpdateInfo.Version;

            if (!await _dialogService.AskAsync(
                string.Format(_loc["AppUpdateDialog"], currentVer, displayLatest),
                _loc["AppUpdateDownloadTitle"]))
            {
                return;
            }

            IsAppUpdateChecking = true;
            AppUpdateStatusMessage = _loc["AppUpdateDownloading"];

            try
            {
                var progress = new Progress<DownloadProgress>(p =>
                {
                    if (!string.IsNullOrEmpty(p.StatusMessage))
                        AppUpdateStatusMessage = p.StatusMessage;
                });

                string downloadedPath = await _updateService.DownloadAppUpdateAsync(_appUpdateInfo, progress);

                if (!File.Exists(downloadedPath))
                    throw new FileNotFoundException(downloadedPath);

                if (!await _dialogService.AskAsync(_loc["AppUpdateReady"], _loc["AppRestartTitle"]))
                    return;

                string appExe = Environment.ProcessPath
                    ?? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "VidDownload.WPF.exe");
                string updaterPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Updater.exe");

                if (!File.Exists(updaterPath))
                {
                    _messageService.Error(string.Format(_loc["UpdaterNotFound"], updaterPath), _loc["UpdateErrorTitle"]);
                    return;
                }

                var updaterInfo = new ProcessStartInfo
                {
                    FileName = updaterPath,
                    Arguments = $"--src \"{downloadedPath}\" --dst \"{appExe}\" --pid {Environment.ProcessId}",
                    UseShellExecute = true,
                    Verb = "runas",
                    CreateNoWindow = true
                };

                Process.Start(updaterInfo);
                _queue.CancelAll();
                System.Windows.Application.Current.Shutdown();
            }
            catch (Exception ex)
            {
                AppUpdateStatusMessage = string.Format(_loc["AppUpdateError"], ex.Message);
                _messageService.Error(string.Format(_loc["AppUpdateError"], ex.Message), _loc["UpdateErrorTitle"]);
            }
            finally
            {
                IsAppUpdateChecking = false;
            }
        }

        private static string GetAppVersion()
        {
            var version = System.Reflection.Assembly.GetEntryAssembly()?.GetName()?.Version;
            return version?.ToString() ?? "0.0.0";
        }
    }
}
