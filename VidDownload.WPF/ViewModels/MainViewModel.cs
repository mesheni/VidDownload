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
        private bool _isVideoOptionsVisible = true;

        [ObservableProperty]
        private bool _isAudioOptionsVisible;

        [ObservableProperty]
        private string _linkLabelText = LocalizedStrings.Instance["LinkLabelVideo"];

        [ObservableProperty]
        private string _selectedLanguage = "RU";

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
        private int _maxConcurrentDownloads = 1;

        [ObservableProperty]
        private bool _isClipboardMonitorEnabled;

        [ObservableProperty]
        private bool _hasActiveDownloads;

        [ObservableProperty]
        private bool _hasQueueItems;

        /// <summary>Предпочтение темы интерфейса (Авто/Светлая/Тёмная) — иконка-переключатель в заголовке.</summary>
        [ObservableProperty]
        private AppThemePreference _appTheme = AppThemePreference.Dark;

        /// <summary>Иконка переключателя темы в заголовке окна.</summary>
        public Wpf.Ui.Controls.SymbolRegular ThemeSymbol => AppTheme switch
        {
            AppThemePreference.Light => Wpf.Ui.Controls.SymbolRegular.WeatherSunny24,
            AppThemePreference.Dark => Wpf.Ui.Controls.SymbolRegular.WeatherMoon24,
            _ => Wpf.Ui.Controls.SymbolRegular.DesktopMac24
        };

        /// <summary>Подсказка переключателя темы.</summary>
        public string ThemeTooltip => $"{_loc["AppearanceLabel"]}: {AppTheme switch
        {
            AppThemePreference.Light => _loc["ThemeLight"],
            AppThemePreference.Dark => _loc["ThemeDark"],
            _ => _loc["ThemeAuto"]
        }}";

        partial void OnAppThemeChanged(AppThemePreference value)
        {
            OnPropertyChanged(nameof(ThemeSymbol));
            OnPropertyChanged(nameof(ThemeTooltip));
        }

        /// <summary>Авто → Светлая → Тёмная → Авто. Применяет и сохраняет тему.</summary>
        [RelayCommand]
        private void CycleAppTheme()
        {
            AppTheme = AppTheme switch
            {
                AppThemePreference.Auto => AppThemePreference.Light,
                AppThemePreference.Light => AppThemePreference.Dark,
                _ => AppThemePreference.Auto
            };
            UiThemeService.SetPreference(AppTheme);
            if (!_isLoading)
                RunSafe(SaveSettingsAsync, nameof(SaveSettingsAsync));
        }

        /// <summary>Агрегат в заголовке секции очереди: «1 загружается · 2 в очереди».</summary>
        [ObservableProperty]
        private string _queueSummaryText = string.Empty;

        /// <summary>Статус загрузки обновления yt-dlp (для строки статуса в футере).</summary>
        [ObservableProperty]
        private string _ytDlpStatusMessage = string.Empty;

        private AppUpdateInfo? _appUpdateInfo;

        /// <summary>Первая непустая строка статуса обновлений — показывается в футере.</summary>
        public string UpdateStatusText
        {
            get
            {
                if (!string.IsNullOrEmpty(YtDlpStatusMessage))
                    return YtDlpStatusMessage;
                if (!string.IsNullOrEmpty(AppUpdateStatusMessage))
                    return AppUpdateStatusMessage;
                return FfmpegStatusMessage;
            }
        }

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
            _queue.Items.CollectionChanged += (_, _) =>
            {
                HasQueueItems = _queue.Items.Count > 0;
                RefreshActivity();
            };

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

        partial void OnYtDlpStatusMessageChanged(string value) => OnPropertyChanged(nameof(UpdateStatusText));

        partial void OnAppUpdateStatusMessageChanged(string value) => OnPropertyChanged(nameof(UpdateStatusText));

        partial void OnFfmpegStatusMessageChanged(string value) => OnPropertyChanged(nameof(UpdateStatusText));

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
            RefreshActivity();
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

        partial void OnMaxConcurrentDownloadsChanged(int value)
        {
            if (_isLoading)
                return;
            var clamped = Math.Clamp(value <= 0 ? 1 : value, 1, 3);
            if (clamped != value)
            {
                MaxConcurrentDownloads = clamped;
                return;
            }
            _queue.MaxConcurrent = clamped;
            RunSafe(SaveSettingsAsync, nameof(SaveSettingsAsync));
        }

        // ==== Очередь загрузок ====

        [RelayCommand]
        private async Task DownloadAsync()
        {
            if (string.IsNullOrWhiteSpace(Url))
            {
                _notifications.Info(_loc["EmptyLink"]);
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

        // ==== Пакетный импорт ссылок ====

        [RelayCommand]
        private void ImportList()
        {
            var dialog = new OpenFileDialog
            {
                Title = _loc["ImportListDialogTitle"],
                Filter = _loc["ImportListFilter"]
            };

            if (dialog.ShowDialog() == true)
                ImportUrlsFromFile(dialog.FileName);
        }

        /// <summary>Ставит в очередь распознанные ссылки из текстового файла (по одной в строке).</summary>
        public void ImportUrlsFromFile(string path)
        {
            try
            {
                ImportUrlsFromLines(File.ReadAllLines(path));
            }
            catch (Exception ex)
            {
                _messageService.Error(string.Format(_loc["ErrorWithMessage"], ex.Message), _loc["ErrorTitle"]);
            }
        }

        /// <summary>Ставит в очередь распознанные ссылки из текста (drag&drop, несколько строк).</summary>
        public void ImportUrlsFromText(string text)
        {
            ImportUrlsFromLines((text ?? string.Empty).Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries));
        }

        private void ImportUrlsFromLines(string[] lines)
        {
            var trimmed = lines.Select(l => l.Trim()).Where(l => l.Length > 0).ToList();
            var urls = trimmed.Where(UrlHelper.LooksLikeVideoReference).Distinct().ToList();

            if (urls.Count == 0)
            {
                _notifications.Info(_loc["NoValidLinksInList"]);
                return;
            }

            foreach (var url in urls)
                Enqueue(url);

            _notifications.Info(string.Format(_loc["ImportedCountNotify"], urls.Count, trimmed.Count));
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

        /// <summary>Повторная загрузка упавшего или отменённого элемента с его исходными параметрами.</summary>
        [RelayCommand]
        private void RetryItem(DownloadItem? item)
        {
            if (item == null || !item.CanRetry)
                return;
            _queue.Enqueue(new DownloadItem(item.Url, item.Options.Clone(), item.IsPlaylist, item.IsAudioOnly, item.IsReEncode));
        }

        /// <summary>Открывает проводник с выделенным скачанным файлом (или папку сохранения).</summary>
        [RelayCommand]
        private void OpenItemLocation(DownloadItem? item)
        {
            if (item == null)
                return;
            if (!string.IsNullOrEmpty(item.FilePath) && File.Exists(item.FilePath))
                Process.Start("explorer.exe", $"/select,\"{item.FilePath}\"");
            else if (Directory.Exists(item.Options.SavePath))
                Process.Start("explorer.exe", $"\"{item.Options.SavePath}\"");
        }

        private void OnItemStarted(object? sender, DownloadItem item)
        {
            RefreshActivity();
        }

        private void OnItemCompleted(object? sender, DownloadItem item)
        {
            RefreshActivity();
            _notifications.Success(string.Format(_loc["DownloadCompleteNotify"], item.DisplayTitle));
            RunSafe(() => AddHistoryAsync(item, DownloadStatus.Completed), nameof(AddHistoryAsync));
        }

        private void OnItemFailed(object? sender, DownloadItem item)
        {
            RefreshActivity();
            _notifications.Error(string.Format(_loc["DownloadFailedNotify"], item.DisplayTitle));
            RunSafe(() => AddHistoryAsync(item, DownloadStatus.Failed), nameof(AddHistoryAsync));
        }

        private void OnItemCancelled(object? sender, DownloadItem item)
        {
            RefreshActivity();
            // Не записываем в историю элементы, которые ни разу не запускались
            if (item.Started)
                RunSafe(() => AddHistoryAsync(item, DownloadStatus.Cancelled), nameof(AddHistoryAsync));
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

        /// <summary>Обновляет агрегаты очереди: признак активности и сводку в заголовке секции.</summary>
        private void RefreshActivity()
        {
            HasActiveDownloads = _queue.HasActiveDownloads;

            int downloading = _queue.Items.Count(i => i.Status == DownloadItemStatus.Downloading);
            int queued = _queue.Items.Count(i => i.Status == DownloadItemStatus.Queued);
            var parts = new List<string>(2);
            if (downloading > 0)
                parts.Add(string.Format(_loc["QueueActiveCount"], downloading));
            if (queued > 0)
                parts.Add(string.Format(_loc["QueueQueuedCount"], queued));
            QueueSummaryText = string.Join("  ·  ", parts);
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
            AppTheme = UiThemeService.TryParse(userSettings.Appearance);
            UiThemeService.SetPreference(AppTheme);
            MaxConcurrentDownloads = Math.Clamp(userSettings.MaxConcurrentDownloads <= 0 ? 1 : userSettings.MaxConcurrentDownloads, 1, 3);
            _queue.MaxConcurrent = MaxConcurrentDownloads;

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
                ClipboardMonitorEnabled = IsClipboardMonitorEnabled,
                Appearance = AppTheme.ToString()
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
                    if (!string.IsNullOrEmpty(p.StatusMessage))
                        YtDlpStatusMessage = p.StatusMessage;
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
                YtDlpStatusMessage = string.Empty;
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
