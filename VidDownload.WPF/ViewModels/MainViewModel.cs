using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
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
        private readonly IYtDlpService _ytDlpService;
        private bool _isLoading;
        private string _savePath = UserSettings.DefaultDownloadPath;

        /// <summary>Последний загруженный снимок настроек — источник сетевых параметров (куки/прокси/ретраи/архив).</summary>
        private UserSettings? _persisted;

        /// <summary>Собранный --download-sections (валидируется в ValidateOptionsAsync).</summary>
        private string _sectionValue = string.Empty;

        /// <summary>Событие: элемент успешно добавлен в очередь (окно возвращает фокус в поле ссылки).</summary>
        public event EventHandler? DownloadEnqueued;

        /// <summary>Событие: нужно перевести фокус на поле ссылки (ошибка валидации).</summary>
        public event EventHandler? FocusUrlRequested;

        [ObservableProperty]
        private string _url = string.Empty;

        /// <summary>Встроенная ошибка поля ссылки (показывается под полем, не снекбаром).</summary>
        [ObservableProperty]
        private string _urlError = string.Empty;

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
        private MainNavigationPage _currentPage = MainNavigationPage.Downloads;

        public bool IsDownloadsPage => CurrentPage == MainNavigationPage.Downloads;
        public bool IsHistoryPage => CurrentPage == MainNavigationPage.History;
        public bool IsConverterPage => CurrentPage == MainNavigationPage.Converter;

        [ObservableProperty]
        private int _activeDownloadsBadgeCount;

        [ObservableProperty]
        private bool _hasActiveBadge;

        public bool IsVideoTabSelected
        {
            get => !IsAudioOnly;
            set
            {
                if (value && IsAudioOnly)
                    IsAudioOnly = false;
                OnPropertyChanged(nameof(IsVideoTabSelected));
                OnPropertyChanged(nameof(IsAudioTabSelected));
            }
        }

        public bool IsAudioTabSelected
        {
            get => IsAudioOnly;
            set
            {
                if (value && !IsAudioOnly)
                    IsAudioOnly = true;
                OnPropertyChanged(nameof(IsVideoTabSelected));
                OnPropertyChanged(nameof(IsAudioTabSelected));
            }
        }

        private ConvertViewModel? _convertVm;
        public ConvertViewModel ConvertVm => _convertVm ??= AppServices.ServiceProvider.GetRequiredService<ConvertViewModel>();

        private HistoryViewModel? _historyVm;
        public HistoryViewModel HistoryVm => _historyVm ??= AppServices.ServiceProvider.GetRequiredService<HistoryViewModel>();

        [ObservableProperty]
        private string _linkLabelText = LocalizedStrings.Instance["LinkLabelVideo"];

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(FfmpegDisplayVersion))]
        private string _ffmpegVersion = LocalizedStrings.Instance["FFmpegChecking"];

        public string FfmpegDisplayVersion
        {
            get
            {
                if (string.IsNullOrEmpty(_ffmpegVersion)) return "ffmpeg";
                if (_ffmpegVersion.Contains("провер", StringComparison.OrdinalIgnoreCase) ||
                    _ffmpegVersion.Contains("check", StringComparison.OrdinalIgnoreCase))
                    return "ffmpeg ...";
                string v = _ffmpegVersion.TrimStart('n', 'v', ' ');
                int dash = v.IndexOf('-');
                if (dash > 0) v = v.Substring(0, dash);
                return $"ffmpeg {v}";
            }
        }

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(CanManageFfmpegUpdate))]
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
        private bool _isEmbedThumbnail;

        [ObservableProperty]
        private bool _isEmbedMetadata;

        [ObservableProperty]
        private bool _isConvertSubsToSrt;

        [ObservableProperty]
        private string _selectedAudioQuality = string.Empty;

        [ObservableProperty]
        private string _sectionStart = string.Empty;

        [ObservableProperty]
        private string _sectionEnd = string.Empty;

        [ObservableProperty]
        private bool _isFetchingInfo;

        [ObservableProperty]
        private string _appVersion = string.Empty;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(CanManageAppUpdate))]
        private bool _isAppUpdateChecking;

        [ObservableProperty]
        private bool _isAppUpdateAvailable;

        [ObservableProperty]
        private string _appUpdateStatusMessage = string.Empty;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(CanManageAppUpdate))]
        private bool _isAppUpdateDownloading;

        [ObservableProperty]
        private int _appUpdateProgressPercent;

        [ObservableProperty]
        private int _ytDlpUpdateProgressPercent;

        [ObservableProperty]
        private int _ffmpegUpdateProgressPercent;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(CanManageYtDlpUpdate))]
        private bool _isYtDlpUpdating;

        [ObservableProperty]
        private bool _isYtDlpDownloading;

        [ObservableProperty]
        private bool _isFfmpegDownloading;

        public bool CanManageAppUpdate => !IsAppUpdateChecking && !IsAppUpdateDownloading;
        public bool CanManageYtDlpUpdate => !IsYtDlpUpdating;
        public bool CanManageFfmpegUpdate => !IsFfmpegChecking;

        [ObservableProperty]
        private string _ytDlpVersion = string.Empty;

        [RelayCommand]
        private void OpenUpdates() => new UpdatesWindow(this).ShowDialog();

        [RelayCommand]
        private Task RefreshAppUpdateAsync() => CheckAppUpdateAsync();

        [RelayCommand]
        private Task UpdateYtDlpAsync() => CheckUpdateAsync();

        /// <summary>Карточка видна как до подтверждения, так и во время загрузки обновления.</summary>
        public bool ShowAppUpdateCard => IsAppUpdateAvailable || IsAppUpdateDownloading;

        /// <summary>Раскрыт ли блок редко используемых параметров загрузки (только в памяти).</summary>
        [ObservableProperty]
        private bool _advancedOptionsExpanded;

        /// <summary>Видимость InfoBar со стартовыми уведомлениями об обновлениях.</summary>
        [ObservableProperty]
        private bool _isUpdateBarOpen;

        [ObservableProperty]
        private string _updateBarTitle = string.Empty;

        [ObservableProperty]
        private string _updateBarMessage = string.Empty;

        [ObservableProperty]
        private Wpf.Ui.Controls.InfoBarSeverity _updateBarSeverity = Wpf.Ui.Controls.InfoBarSeverity.Informational;

        [ObservableProperty]
        private string _savePathText = string.Empty;

        [ObservableProperty]
        private string _rateLimit = string.Empty;

        /// <summary>Встроенная ошибка лимита скорости (под полем).</summary>
        [ObservableProperty]
        private string _rateLimitError = string.Empty;

        [ObservableProperty]
        private bool _minimizeToTray;

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

        /// <summary>Счётчик стартовых проверок обновлений: InfoBar закрывается, когда все закончились.</summary>
        private int _startupChecksPending;

        /// <summary>true, если стартовая проверка уже нашла важное сообщение — его не перекрываем.</summary>
        private bool _startupBarActionable;

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

        /// <summary>Подпись кнопки «Скачать»: во время получения метаданных — «Получаем информацию…».</summary>
        public string DownloadButtonText => IsFetchingInfo ? _loc["FetchingInfo"] : _loc["DownloadButton"];

        /// <summary>Кнопка «Скачать» доступна, когда нет активного запроса метаданных.
        /// Проверка пустой ссылки — внутри DownloadAsync (показывает встроенную ошибку).</summary>
        public bool CanDownload => !IsFetchingInfo;

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

        /// <summary>
        /// Качество аудио при извлечении (--audio-quality). Отображается человекочитаемой
        /// подписью, но в настройки уходит исходное значение (SelectedValuePath="Value").
        /// </summary>
        public ObservableCollection<AudioQualityOption> AudioQualities { get; } = new();

        /// <summary>Ключи действий «после очереди» — индекс синхронен с PostQueueActionItems.</summary>
        private static readonly string[] PostQueueActionKeys = { "", "Shutdown", "Sleep", "Hibernate" };

        /// <summary>Локализованные подписи действий «после очереди» (пересобирается при смене языка).</summary>
        public ObservableCollection<string> PostQueueActionItems { get; } = new();

        /// <summary>Выбранное действие после завершения очереди (индекс в PostQueueActionItems).</summary>
        [ObservableProperty]
        private int _selectedPostQueueIndex;

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
            IYtDlpService ytDlpService,
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
            _ytDlpService = ytDlpService;

            BuildAudioQualityItems();

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
            _loc.PropertyChanged += OnLocalizedStringsChanged;
            BuildPostQueueActionItems();

            // Стартовые проверки не открывают модальные диалоги — результат уходит в InfoBar
            RunSafe(() => CheckUpdateCoreAsync(interactive: false), nameof(CheckUpdateAsync));
            RunSafe(() => CheckFFmpegUpdateCoreAsync(interactive: false), nameof(CheckFFmpegUpdateAsync));
            RunSafe(() => CheckAppUpdateCoreAsync(interactive: false), nameof(CheckAppUpdateAsync));
            RunSafe(LoadSettingsAsync, nameof(LoadSettingsAsync));
            RunSafe(RestorePendingQueueAsync, nameof(RestorePendingQueueAsync));
            AppVersion = GetAppVersion();
        }

        /// <summary>Восстанавливает очередь из прошлого сеанса (все элементы — в паузе).</summary>
        private Task RestorePendingQueueAsync()
        {
            var items = _queue.LoadPending();
            foreach (var item in items)
                _queue.Items.Add(item);
            if (items.Count > 0)
                _notifications.Info(string.Format(_loc["QueueRestored"], items.Count));
            return Task.CompletedTask;
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

        // ==== InfoBar стартовых проверок обновлений ====

        /// <summary>Стартовые проверки показывают только первое сообщение; важное не перекрывается.</summary>
        internal static bool ShouldPublishStartupUpdate(bool hasActionableMessage)
            => !hasActionableMessage;

        /// <summary>Отмечает начало стартовой проверки и показывает «проверяем», пока нет важного сообщения.</summary>
        private void BeginStartupCheck()
        {
            _startupChecksPending++;
            if (ShouldPublishStartupUpdate(_startupBarActionable))
                PublishUpdateBar(_loc["UpdateBarChecking"], Wpf.Ui.Controls.InfoBarSeverity.Informational);
        }

        /// <summary>Закрывает InfoBar, когда все стартовые проверки закончились без важных сообщений.</summary>
        private void EndStartupCheck()
        {
            _startupChecksPending = Math.Max(0, _startupChecksPending - 1);
            if (_startupChecksPending == 0 && !_startupBarActionable)
                IsUpdateBarOpen = false;
        }

        /// <summary>Публикует стартовое уведомление в InfoBar (первое важное сообщение выигрывает).</summary>
        private void PublishUpdateBar(string message, Wpf.Ui.Controls.InfoBarSeverity severity)
        {
            if (!ShouldPublishStartupUpdate(_startupBarActionable))
                return;

            _startupBarActionable = severity == Wpf.Ui.Controls.InfoBarSeverity.Warning;
            UpdateBarTitle = _loc["UpdateBarTitle"];
            UpdateBarMessage = message;
            UpdateBarSeverity = severity;
            IsUpdateBarOpen = true;
        }

        partial void OnYtDlpStatusMessageChanged(string value) => OnPropertyChanged(nameof(UpdateStatusText));

        partial void OnAppUpdateStatusMessageChanged(string value) => OnPropertyChanged(nameof(UpdateStatusText));

        partial void OnIsAppUpdateAvailableChanged(bool value) => OnPropertyChanged(nameof(ShowAppUpdateCard));

        partial void OnIsAppUpdateDownloadingChanged(bool value) => OnPropertyChanged(nameof(ShowAppUpdateCard));

        partial void OnFfmpegStatusMessageChanged(string value) => OnPropertyChanged(nameof(UpdateStatusText));

        partial void OnCurrentPageChanged(MainNavigationPage value)
        {
            OnPropertyChanged(nameof(IsDownloadsPage));
            OnPropertyChanged(nameof(IsHistoryPage));
            OnPropertyChanged(nameof(IsConverterPage));

            if (value == MainNavigationPage.History)
            {
                _ = HistoryVm.LoadAsync();
            }
            else if (value == MainNavigationPage.Converter)
            {
                _ = ConvertVm.InitializeAsync();
            }
        }

        partial void OnIsAudioOnlyChanged(bool value)
        {
            IsVideoOptionsVisible = !value;
            IsAudioOptionsVisible = value;
            OnPropertyChanged(nameof(IsVideoTabSelected));
            OnPropertyChanged(nameof(IsAudioTabSelected));
        }

        partial void OnIsPlaylistChanged(bool value)
        {
            LinkLabelText = value ? _loc["LinkLabelPlaylist"] : _loc["LinkLabelVideo"];
        }

        partial void OnUrlChanged(string value)
        {
            // Ошибка поля живёт только до первого изменения ссылки
            if (!string.IsNullOrEmpty(UrlError))
                UrlError = string.Empty;
            DownloadCommand.NotifyCanExecuteChanged();
        }

        partial void OnIsFetchingInfoChanged(bool value)
        {
            OnPropertyChanged(nameof(DownloadButtonText));
            DownloadCommand.NotifyCanExecuteChanged();
        }

        private void OnLocalizedStringsChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (!string.IsNullOrEmpty(e.PropertyName))
                return;

            BuildPostQueueActionItems();
            BuildAudioQualityItems();
            LinkLabelText = IsPlaylist ? _loc["LinkLabelPlaylist"] : _loc["LinkLabelVideo"];
            OnPropertyChanged(nameof(UpdateStatusText));
            OnPropertyChanged(nameof(DownloadButtonText));
            OnPropertyChanged(nameof(ThemeTooltip));
            RefreshActivity();

            // Конвертеры/строки, вычисленные внутри элемента, тоже должны перечитаться
            foreach (var item in _queue.Items)
                item.NotifyLanguageChanged();
        }

        private void BuildPostQueueActionItems()
        {
            int selected = SelectedPostQueueIndex;
            PostQueueActionItems.Clear();
            PostQueueActionItems.Add(_loc["PostQueueNone"]);
            PostQueueActionItems.Add(_loc["PostQueueShutdown"]);
            PostQueueActionItems.Add(_loc["PostQueueSleep"]);
            PostQueueActionItems.Add(_loc["PostQueueHibernate"]);
            SelectedPostQueueIndex = selected;
        }

        /// <summary>Пересобирает список качества аудио с локализованными подписями.</summary>
        private void BuildAudioQualityItems()
        {
            string selected = SelectedAudioQuality;
            AudioQualities.Clear();
            AudioQualities.Add(new AudioQualityOption(string.Empty, string.Empty));
            foreach (var value in new[] { "0", "3", "5", "7", "9" })
                AudioQualities.Add(new AudioQualityOption(GetAudioQualityDisplay(value, _loc.GetString), value));
            SelectedAudioQuality = selected;
        }

        /// <summary>Человекочитаемая подпись качества аудио; незнакомые значения показываются как есть.</summary>
        internal static string GetAudioQualityDisplay(string? value, Func<string, string> getString)
            => value switch
            {
                "0" => getString("AudioQualityBest"),
                "9" => getString("AudioQualityWorst"),
                _ => value ?? string.Empty
            };

        /// <summary>Берёт сохранённое значение, если оно есть в списке, иначе — значение по умолчанию.</summary>
        internal static string ResolveSelection(string? saved, IList<string> items, string fallback)
        {
            if (!string.IsNullOrEmpty(saved) && items.Contains(saved))
                return saved;
            return items.Contains(fallback) ? fallback : string.Empty;
        }

        /// <summary>Допустимый --limit-rate: число с необязательной десятичной частью и суффиксом K/M/G.</summary>
        internal static bool IsValidRateLimit(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return true;
            return Regex.IsMatch(value.Trim(), @"^\d+(\.\d+)?[KkMmGg]?$");
        }

        /// <summary>Первая строка ошибки, не длиннее 100 символов — короткая причина для снекбара.</summary>
        internal static string ShortenFailureReason(string? error)
        {
            if (string.IsNullOrWhiteSpace(error))
                return string.Empty;

            string firstLine = (error ?? string.Empty)
                .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(line => line.Trim())
                .FirstOrDefault(line => line.Length > 0) ?? string.Empty;

            const int maxLength = 100;
            return firstLine.Length > maxLength ? firstLine[..maxLength] + "…" : firstLine;
        }

        /// <summary>Сводка очереди в заголовке секции, включая приостановленные загрузки.</summary>
        internal static string BuildQueueSummary(int downloading, int queued, int paused, Func<string, string> getString)
        {
            var parts = new List<string>(3);
            if (downloading > 0)
                parts.Add(string.Format(getString("QueueActiveCount"), downloading));
            if (queued > 0)
                parts.Add(string.Format(getString("QueueQueuedCount"), queued));
            if (paused > 0)
                parts.Add(string.Format(getString("QueueSummaryPaused"), paused));
            return string.Join("  ·  ", parts);
        }

        partial void OnSelectedPostQueueIndexChanged(int value)
        {
            if (_isLoading)
                return;
            RunSafe(SaveSettingsAsync, nameof(SaveSettingsAsync));
        }

        /// <summary>Ключ выбранного действия после очереди ("", Shutdown, Sleep, Hibernate).</summary>
        private string PostQueueAction => PostQueueActionKeys[Math.Clamp(SelectedPostQueueIndex, 0, PostQueueActionKeys.Length - 1)];

        partial void OnIsReEncodeChanged(bool value)
        {
            if (value)
            {
                if (!Formats.Contains("mov"))
                    Formats.Add("mov");
                return;
            }

            // Сначала уводим выбор с удаляемого элемента, потом убираем его из списка,
            // иначе WPF выставит SelectedFormat = null и следующий ApplySelections упадёт
            if (string.Equals(SelectedFormat, "mov", StringComparison.Ordinal))
                SelectedFormat = ResolveSelection(null, Formats, "mp4");
            Formats.Remove("mov");
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

            if (!IsValidRateLimit(value))
            {
                // Некорректное значение не сохраняем и не передаём в yt-dlp
                RateLimitError = _loc["RateLimitInvalid"];
                return;
            }

            RateLimitError = string.Empty;
            _settings.RateLimit = value?.Trim() ?? string.Empty;
            RunSafe(SaveSettingsAsync, nameof(SaveSettingsAsync));
        }

        partial void OnMinimizeToTrayChanged(bool value)
        {
            if (_isLoading)
                return;
            RunSafe(SaveSettingsAsync, nameof(SaveSettingsAsync));
        }

        // ==== Очередь загрузок ====

        [RelayCommand(CanExecute = nameof(CanDownload))]
        private async Task DownloadAsync()
        {
            // Повторный Enter во время запроса метаданных не должен запускать вторую загрузку
            if (IsFetchingInfo)
                return;

            if (string.IsNullOrWhiteSpace(Url))
            {
                // Встроенная валидация под полем + фокус на поле вместо снекбара
                UrlError = _loc["EmptyLink"];
                FocusUrlRequested?.Invoke(this, EventArgs.Empty);
                return;
            }

            UrlError = string.Empty;

            if (!UrlHelper.LooksLikeVideoReference(Url) &&
                !await _dialogService.AskAsync(_loc["InvalidUrlWarning"], _loc["WarningTitle"]))
            {
                return;
            }

            if (!await ValidateOptionsAsync())
                return;

            if (await EnqueueWithPreviewAsync())
            {
                Url = string.Empty;
                DownloadEnqueued?.Invoke(this, EventArgs.Empty);
            }
        }

        /// <summary>
        /// Предпросмотр метаданных (название, обложка, форматы/элементы плейлиста) и постановка в очередь.
        /// При недоступности предпросмотра предлагает скачать напрямую.
        /// </summary>
        private async Task<bool> EnqueueWithPreviewAsync()
        {
            ApplySelectionsToSettings();
            _settings.FormatSelector = string.Empty;
            _settings.PlaylistItems = string.Empty;

            // Настройка «не показывать предпросмотр»: сразу в очередь, без запроса метаданных
            if (_persisted is not { ShowMetadataPreview: false })
            {
                VideoInfo? info = null;
                IsFetchingInfo = true;
                try
                {
                    info = await _ytDlpService.FetchInfoAsync(Url, IsPlaylist, settings: _settings.Clone());
                }
                catch (Exception ex)
                {
                    AppLog.Error(nameof(MainViewModel), $"FetchInfo failed: {ex.Message}");
                    if (!await _dialogService.AskAsync(string.Format(_loc["PreviewFailedAsk"], ex.Message), _loc["WarningTitle"]))
                        return false;
                }
                finally
                {
                    IsFetchingInfo = false;
                }

                if (info != null)
                {
                    var window = AppServices.ServiceProvider.GetRequiredService<VideoInfoWindow.VideoInfoWindow>();
                    window.Owner = System.Windows.Application.Current.MainWindow;
                    window.ViewModel.Initialize(info, IsAudioOnly);
                    window.ViewModel.DownloadSettingsSummary = IsAudioOnly
                        ? $"{_loc["AudioCodecLabel"]} {_settings.AudioCodec} · {_settings.AudioQuality}"
                        : $"{_loc["ResolutionLabel"]} {_settings.Resolution}p · {_settings.VideoCodec} · {_settings.Format.ToUpperInvariant()}";
                    window.ShowDialog();
                    if (!window.ViewModel.Confirmed)
                        return false;
                    _settings.PlaylistItems = window.ViewModel.PlaylistItems;
                }
            }

            _queue.Enqueue(new DownloadItem(Url, _settings.Clone(), IsPlaylist, IsAudioOnly, IsReEncode));
            RunSafe(SaveSettingsAsync, nameof(SaveSettingsAsync));
            return true;
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
            // Null/пустые значения возможны, когда элемент исчез из списка (например, mov)
            if (!string.IsNullOrEmpty(SelectedResolution))
                _settings.Resolution = SelectedResolution;
            if (!string.IsNullOrEmpty(SelectedAudioFormat))
                _settings.AudioCodec = SelectedAudioFormat;
            if (!string.IsNullOrEmpty(SelectedFormat))
                _settings.Format = SelectedFormat;
            if (!string.IsNullOrEmpty(SelectedCodec) && Codecs.Contains(SelectedCodec))
                _settings.VideoCodec = SelectedCodec;

            _settings.DownloadSubtitles = IsDownloadSubtitles;
            _settings.SubtitleLanguage = string.IsNullOrEmpty(SelectedSubtitleLanguage) ? "all" : SelectedSubtitleLanguage;
            // Для аудио субтитры не встраиваются: опцию игнорируем, не меняя состояние чекбокса
            _settings.EmbedSubtitles = !IsAudioOnly && IsEmbedSubtitles;
            _settings.ConvertSubsToSrt = IsConvertSubsToSrt;
            _settings.EmbedThumbnail = IsEmbedThumbnail;
            _settings.EmbedMetadata = IsEmbedMetadata;
            _settings.AudioQuality = SelectedAudioQuality ?? string.Empty;
            _settings.DownloadSections = _sectionValue;
            _settings.SavePath = _savePath;
            // Некорректный лимит скорости не передаём в yt-dlp
            _settings.RateLimit = IsValidRateLimit(RateLimit) ? RateLimit?.Trim() ?? string.Empty : string.Empty;

            // Сетевые параметры принадлежат окну настроек — берём последний снимок оттуда
            _settings.CookiesFromBrowser = _persisted?.CookiesFromBrowser ?? string.Empty;
            _settings.CookiesFile = _persisted?.CookiesFile ?? string.Empty;
            _settings.Proxy = _persisted?.Proxy ?? string.Empty;
            _settings.Retries = _persisted?.Retries ?? 0;
            _settings.UseDownloadArchive = _persisted?.UseDownloadArchive ?? false;
        }

        private async Task<bool> ValidateOptionsAsync()
        {
            // Аудио-режим просто игнорирует встраивание субтитров (чекбокс не трогаем),
            // поэтому предупреждение нужно только для видео с контейнером AVI
            if (!IsAudioOnly && IsEmbedSubtitles && string.Equals(SelectedFormat, "avi", StringComparison.OrdinalIgnoreCase))
            {
                if (!await _dialogService.AskAsync(_loc["AviSubtitleWarning"], _loc["WarningTitle"]))
                    return false;
            }

            // Фрагмент по таймкодам: либо оба поля, либо ни одного
            if (!string.IsNullOrWhiteSpace(SectionStart) || !string.IsNullOrWhiteSpace(SectionEnd))
            {
                if (!Timecodes.TryBuildSection(SectionStart, SectionEnd, out _sectionValue))
                {
                    _messageService.Warning(_loc["SectionInvalid"], _loc["WarningTitle"]);
                    return false;
                }
            }
            else
            {
                _sectionValue = string.Empty;
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

        /// <summary>«Очистить завершённые» доступна, только когда есть что очищать.</summary>
        private bool CanClearFinished() => _queue.Items.Any(i => i.IsFinished);

        [RelayCommand(CanExecute = nameof(CanClearFinished))]
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
            RunSafe(CheckPostQueueActionAsync, nameof(CheckPostQueueActionAsync));
        }

        /// <summary>
        /// Действие после очереди (выключение/сон/гибернация): спрашивает подтверждение,
        /// когда после завершения загрузки в очереди не осталось активных элементов.
        /// </summary>
        private async Task CheckPostQueueActionAsync()
        {
            if (PostQueueAction.Length == 0)
                return;

            if (_queue.Items.Any(i => i.Status is DownloadItemStatus.Downloading
                or DownloadItemStatus.Queued
                or DownloadItemStatus.Paused))
                return;

            string actionName = PostQueueActionItems[Math.Clamp(SelectedPostQueueIndex, 0, PostQueueActionItems.Count - 1)];
            string message = PostQueueAction == "Shutdown"
                ? _loc["PostQueueConfirmShutdown"]
                : string.Format(_loc["PostQueueConfirm"], actionName);

            if (!await _dialogService.AskAsync(message, _loc["ConfirmationTitle"]))
                return;

            try
            {
                switch (PostQueueAction)
                {
                    case "Shutdown":
                        // 30 секунд на отмену: shutdown /a
                        Process.Start("shutdown", "/s /t 30");
                        break;
                    case "Sleep":
                        Process.Start("rundll32.exe", "powrprof.dll,SetSuspendState 0,1,0");
                        break;
                    case "Hibernate":
                        Process.Start("rundll32.exe", "powrprof.dll,SetSuspendState 1,1,0");
                        break;
                }
            }
            catch (Exception ex)
            {
                AppLog.Error(nameof(MainViewModel), $"Post-queue action failed: {ex.Message}");
            }
        }

        private void OnItemFailed(object? sender, DownloadItem item)
        {
            RefreshActivity();
            string reason = ShortenFailureReason(item.ErrorMessage);
            _notifications.Error(string.IsNullOrEmpty(reason)
                ? string.Format(_loc["DownloadFailedNotify"], item.DisplayTitle)
                : string.Format(_loc["DownloadFailedWithReason"], item.DisplayTitle, reason));
            RunSafe(() => AddHistoryAsync(item, DownloadStatus.Failed), nameof(AddHistoryAsync));
            if (!item.Options.FormatSelector.Contains("protocol^=m3u8", StringComparison.Ordinal)
                && YtDlpService.CanOfferAlternativeFormat(item.Url, item.ErrorMessage, item.IsAudioOnly, item.IsPlaylist))
                RunSafe(() => OfferAlternativeFormatAsync(item), nameof(OfferAlternativeFormatAsync));
        }

        private async Task OfferAlternativeFormatAsync(DownloadItem item)
        {
            if (!await _dialogService.AskAsync(_loc["AlternativeFormatPrompt"], _loc["AlternativeFormatTitle"]))
                return;

            var options = item.Options.Clone();
            options.FormatSelector = Command.HlsFormatSelector(options.Resolution);
            _queue.Enqueue(new DownloadItem(item.Url, options, false, false, item.IsReEncode));
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
            int paused = _queue.Items.Count(i => i.Status == DownloadItemStatus.Paused);
            QueueSummaryText = BuildQueueSummary(downloading, queued, paused, _loc.GetString);

            ActiveDownloadsBadgeCount = downloading;
            HasActiveBadge = downloading > 0;

            ClearFinishedCommand.NotifyCanExecuteChanged();
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
            try
            {
                if (!Directory.Exists(_savePath))
                    Directory.CreateDirectory(_savePath);
                Process.Start("explorer.exe", $"\"{_savePath}\"");
            }
            catch (Exception ex)
            {
                AppLog.Error(nameof(MainViewModel), $"OpenFolder failed: {ex.Message}");
                _notifications.Error(string.Format(_loc["OpenFolderFailed"], ex.Message));
            }
        }

        [RelayCommand]
        public void ShowDownloads()
        {
            CurrentPage = MainNavigationPage.Downloads;
        }

        [RelayCommand]
        public async Task ShowHistoryAsync()
        {
            CurrentPage = MainNavigationPage.History;
            await HistoryVm.LoadAsync();
        }

        [RelayCommand]
        public async Task ShowConverterAsync()
        {
            CurrentPage = MainNavigationPage.Converter;
            await ConvertVm.InitializeAsync();
        }

        [RelayCommand]
        public void RedownloadHistory(DownloadHistoryEntry? entry)
        {
            if (entry != null && !string.IsNullOrWhiteSpace(entry.Url))
            {
                Url = entry.Url;
                CurrentPage = MainNavigationPage.Downloads;
                DownloadCommand.Execute(null);
            }
        }

        [RelayCommand]
        private void OpenConverter()
        {
            var convert = AppServices.ServiceProvider.GetRequiredService<ConvertWindow.ConvertWindow>();
            convert.Owner = System.Windows.Application.Current.MainWindow;
            convert.ShowDialog();
        }

        [RelayCommand]
        private void OpenSettings()
        {
            var settings = AppServices.ServiceProvider.GetRequiredService<SettingsWindow.SettingsWindow>();
            settings.Owner = System.Windows.Application.Current.MainWindow;
            if (settings.ShowDialog() == true)
                RunSafe(LoadSettingsAsync, nameof(LoadSettingsAsync));
        }

        [RelayCommand]
        private void OpenHelp()
        {
            var help = AppServices.ServiceProvider.GetRequiredService<HelpWindow>();
            help.Owner = System.Windows.Application.Current.MainWindow;
            help.ShowDialog();
        }

        [RelayCommand]
        private void OpenHistory()
        {
            var history = AppServices.ServiceProvider.GetRequiredService<HistoryWindow.HistoryWindow>();
            history.Owner = System.Windows.Application.Current.MainWindow;
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
                () => { Url = url; RunSafe(DownloadAsync, nameof(DownloadAsync)); });
        }

        // ==== Настройки ====

        private async Task LoadSettingsAsync()
        {
            _isLoading = true;
            var userSettings = await _settingsService.LoadAsync();
            _persisted = userSettings;
            // Пустые сохранённые значения не должны оставлять комбобоксы «пустыми»:
            // подставляем те же значения по умолчанию, что использует Settings
            SelectedResolution = ResolveSelection(userSettings.Resolution, Resolutions, "1080");
            SelectedCodec = ResolveSelection(userSettings.VideoCodec, Codecs, "av01");
            SelectedAudioFormat = ResolveSelection(userSettings.AudioCodec, AudioFormats, "aac");
            SelectedFormat = ResolveSelection(userSettings.Format, Formats, "mp4");
            IsDownloadSubtitles = userSettings.DownloadSubtitles;
            if (!string.IsNullOrEmpty(userSettings.SubtitleLanguage))
                SelectedSubtitleLanguage = userSettings.SubtitleLanguage;
            IsEmbedSubtitles = userSettings.EmbedSubtitles;
            IsConvertSubsToSrt = userSettings.ConvertSubsToSrt;
            IsEmbedThumbnail = userSettings.EmbedThumbnail;
            IsEmbedMetadata = userSettings.EmbedMetadata;
            if (!string.IsNullOrEmpty(userSettings.AudioQuality))
                SelectedAudioQuality = userSettings.AudioQuality;
            if (!string.IsNullOrEmpty(userSettings.Language))
                _loc.SetLanguage(userSettings.Language.ToLower());
            _savePath = !string.IsNullOrEmpty(userSettings.SavePath)
                ? userSettings.SavePath
                : UserSettings.DefaultDownloadPath;
            SavePathText = _savePath;
            RateLimit = userSettings.RateLimit ?? string.Empty;
            _settings.RateLimit = RateLimit;
            MinimizeToTray = userSettings.MinimizeToTray;
            AppTheme = UiThemeService.TryParse(userSettings.Appearance);
            UiThemeService.SetPreference(AppTheme);
            _queue.MaxConcurrent = Math.Clamp(userSettings.MaxConcurrentDownloads <= 0 ? 1 : userSettings.MaxConcurrentDownloads, 1, 3);
            _clipboardMonitor.IsEnabled = userSettings.ClipboardMonitorEnabled;
            int postQueueIndex = Array.IndexOf(PostQueueActionKeys,
                string.IsNullOrEmpty(userSettings.PostQueueAction) ? string.Empty : userSettings.PostQueueAction);
            SelectedPostQueueIndex = postQueueIndex < 0 ? 0 : postQueueIndex;

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

            RateLimitError = IsValidRateLimit(RateLimit) ? string.Empty : _loc["RateLimitInvalid"];
            _isLoading = false;
        }

        /// <summary>
        /// Сохраняет только поля, которыми владеет главное окно (параметры загрузки, папка,
        /// лимит скорости, тема). Остальное (язык, трей, буфер, конвертер) читается из файла —
        /// так одновременные правки из окна настроек и конвертера не затираются.
        /// </summary>
        private async Task SaveSettingsAsync()
        {
            ApplySelectionsToSettings();

            var settings = await _settingsService.LoadAsync().ConfigureAwait(true);
            settings.Resolution = _settings.Resolution;
            settings.VideoCodec = _settings.VideoCodec;
            settings.AudioCodec = _settings.AudioCodec;
            settings.Format = _settings.Format;
            settings.DownloadSubtitles = _settings.DownloadSubtitles;
            settings.SubtitleLanguage = _settings.SubtitleLanguage;
            settings.EmbedSubtitles = _settings.EmbedSubtitles;
            settings.ConvertSubsToSrt = _settings.ConvertSubsToSrt;
            settings.EmbedThumbnail = _settings.EmbedThumbnail;
            settings.EmbedMetadata = _settings.EmbedMetadata;
            settings.AudioQuality = SelectedAudioQuality ?? string.Empty;
            settings.SavePath = _savePath;
            settings.RateLimit = IsValidRateLimit(RateLimit) ? RateLimit?.Trim() ?? string.Empty : string.Empty;
            settings.Appearance = AppTheme.ToString();
            settings.MinimizeToTray = MinimizeToTray;
            settings.PostQueueAction = PostQueueAction;
            await _settingsService.SaveAsync(settings).ConfigureAwait(true);
        }

        // ==== Обновление FFmpeg ====

        [RelayCommand]
        private Task CheckFFmpegUpdateAsync() => CheckFFmpegUpdateCoreAsync(interactive: true);

        /// <summary>
        /// Проверка FFmpeg. Ручной запуск (кнопка в футере) спрашивает и скачивает обновление,
        /// стартовая проверка только показывает InfoBar и ничего не скачивает.
        /// </summary>
        private async Task CheckFFmpegUpdateCoreAsync(bool interactive)
        {
            if (IsFfmpegChecking)
                return;

            if (!interactive)
                BeginStartupCheck();

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
                    {
                        FfmpegStatusMessage = _loc["FFmpegUpToDate"];
                    }
                    else
                    {
                        FfmpegStatusMessage = _loc["FFmpegNotFound"];
                        if (!interactive)
                            PublishUpdateBar(_loc["FFmpegNotFound"], Wpf.Ui.Controls.InfoBarSeverity.Warning);
                    }
                    return;
                }

                if (string.IsNullOrEmpty(info.DownloadUrl))
                {
                    if (interactive)
                        _messageService.Error(_loc["FFmpegDownloadLinkError"], _loc["UpdateErrorTitle"]);
                    FfmpegStatusMessage = _loc["FFmpegLinkNotFound"];
                    if (!interactive)
                        PublishUpdateBar(_loc["FFmpegLinkNotFound"], Wpf.Ui.Controls.InfoBarSeverity.Warning);
                    return;
                }

                IsFfmpegUpdateAvailable = true;
                string displayLatest = info.LatestVersion.Length > 30
                    ? info.LatestVersion[..27] + "..."
                    : info.LatestVersion;
                FfmpegStatusMessage = string.Format(_loc["FFmpegVersionAvailable"], displayLatest);

                // Стартовая проверка: только уведомление, без диалога и загрузки
                if (!interactive)
                {
                    PublishUpdateBar(
                        string.Format(_loc["UpdateBarFFmpegMessage"], displayLatest),
                        Wpf.Ui.Controls.InfoBarSeverity.Warning);
                    return;
                }

                string displayCurrent = string.IsNullOrEmpty(localVer) ? _loc["VersionNotFound"] : localVer;
                if (!await _dialogService.AskAsync(
                    string.Format(_loc["FFmpegUpdateDialog"], displayCurrent, displayLatest),
                    _loc["FFmpegUpdateAvailableTitle"]))
                {
                    return;
                }

                FfmpegVersion = _loc["FFmpegUpdating"];
                IsFfmpegDownloading = true;
                FfmpegUpdateProgressPercent = 0;

                var progress = new Progress<DownloadProgress>(p =>
                {
                    FfmpegUpdateProgressPercent = Math.Clamp(p.Percent, 0, 100);
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
                if (interactive)
                    _messageService.Error(string.Format(_loc["FFmpegUpdateFailed"], ex.Message), _loc["UpdateErrorTitle"]);
            }
            finally
            {
                IsFfmpegDownloading = false;
                IsFfmpegChecking = false;
                if (!interactive)
                    EndStartupCheck();
            }
        }

        // ==== Обновление yt-dlp ====

        public Task CheckUpdateAsync() => CheckUpdateCoreAsync(interactive: true);

        /// <summary>
        /// Проверка обновления yt-dlp. Ручной запуск после подтверждения скачивает обновление,
        /// стартовая проверка только показывает InfoBar.
        /// </summary>
        private async Task CheckUpdateCoreAsync(bool interactive)
        {
            if (IsYtDlpUpdating)
                return;
            IsYtDlpUpdating = true;
            YtDlpStatusMessage = _loc["AppCheckingUpdate"];
            if (!interactive)
                BeginStartupCheck();

            try
            {
                UpdateInfo info;

                try
                {
                    info = await _updateService.CheckForUpdateAsync();
                }
                catch (Exception ex)
                {
                    AppLog.Error(nameof(MainViewModel), $"yt-dlp update check failed: {ex.Message}");
                    YtDlpStatusMessage = string.Format(_loc["ErrorWithMessage"], ex.Message);
                    return;
                }

                string currentVer = await _updateService.GetCurrentVersionAsync();
                YtDlpVersion = currentVer;
                YtDlpStatusMessage = info.IsUpdateAvailable
                    ? string.Format(_loc["AppUpdateAvailable"], info.Version)
                    : _loc["AppUpToDate"];
                if (!info.IsUpdateAvailable)
                    return;
                bool fileNotFound = string.IsNullOrEmpty(currentVer);
                string displayCurrent = fileNotFound ? _loc["VersionNotFound"] : currentVer;

                if (!interactive)
                {
                    PublishUpdateBar(
                        string.Format(_loc["UpdateBarYtDlpMessage"], info.Version),
                        Wpf.Ui.Controls.InfoBarSeverity.Warning);
                    return;
                }

                if (!fileNotFound &&
                    !await _dialogService.AskAsync(
                        string.Format(_loc["YtDlpUpdateDialog"], displayCurrent, info.Version),
                        _loc["YtDlpUpdateAvailableTitle"]))
                {
                    return;
                }

                try
                {
                    IsYtDlpDownloading = true;
                    YtDlpUpdateProgressPercent = 0;
                    var progress = new Progress<DownloadProgress>(p =>
                    {
                        YtDlpUpdateProgressPercent = Math.Clamp(p.Percent, 0, 100);
                        if (!string.IsNullOrEmpty(p.StatusMessage))
                            YtDlpStatusMessage = p.StatusMessage;
                    });

                    await _updateService.DownloadUpdateAsync(info, progress);

                    YtDlpVersion = await _updateService.GetCurrentVersionAsync();
                    YtDlpStatusMessage = string.Format(_loc["YtDlpUpdated"], YtDlpVersion);

                    _notifications.Success(string.Format(_loc["YtDlpUpdated"], info.Version), _loc["UpdateCompletedTitle"]);
                }
                catch (Exception ex)
                {
                    YtDlpStatusMessage = string.Format(_loc["ErrorWithMessage"], ex.Message);
                    _notifications.Error(string.Format(_loc["ErrorWithMessage"], ex.Message), _loc["UpdateErrorTitle"]);
                }
            }
            finally
            {
                IsYtDlpDownloading = false;
                IsYtDlpUpdating = false;
                if (!interactive)
                    EndStartupCheck();
            }
        }

        // ==== Обновление приложения ====

        public Task CheckAppUpdateAsync() => CheckAppUpdateCoreAsync(interactive: true);

        /// <summary>
        /// Проверка обновления приложения. Стартовая проверка только заполняет InfoBar,
        /// диалог скачивания живёт в <see cref="UpdateAppAsync"/>.
        /// </summary>
        private async Task CheckAppUpdateCoreAsync(bool interactive)
        {
            if (!CanManageAppUpdate)
                return;

            if (!interactive)
                BeginStartupCheck();

            IsAppUpdateChecking = true;
            AppUpdateStatusMessage = _loc["AppCheckingUpdate"];

            try
            {
                var info = await _updateService.CheckAppUpdateAsync();
                _appUpdateInfo = info;
                IsAppUpdateAvailable = info.IsUpdateAvailable;

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

                if (!interactive)
                    PublishUpdateBar(
                        string.Format(_loc["UpdateBarAppMessage"], displayLatest),
                        Wpf.Ui.Controls.InfoBarSeverity.Warning);
            }
            catch
            {
                AppUpdateStatusMessage = _loc["AppUpdateCheckError"];
            }
            finally
            {
                IsAppUpdateChecking = false;
                if (!interactive)
                    EndStartupCheck();
            }
        }

        [RelayCommand]
        private async Task UpdateAppAsync()
        {
            if (!CanManageAppUpdate)
                return;
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

            IsAppUpdateDownloading = true;
            AppUpdateProgressPercent = 0;
            AppUpdateStatusMessage = _loc["AppUpdateDownloading"];

            try
            {
                var progress = new Progress<DownloadProgress>(p =>
                {
                    AppUpdateProgressPercent = Math.Clamp(p.Percent, 0, 100);
                    if (!string.IsNullOrEmpty(p.StatusMessage))
                        AppUpdateStatusMessage = p.StatusMessage;
                });

                string downloadedPath = await _updateService.DownloadAppUpdateAsync(_appUpdateInfo, progress);

                if (!File.Exists(downloadedPath))
                    throw new FileNotFoundException(downloadedPath);

                // Вместе с приложением обновляем и Updater.exe, если ассет есть в релизе
                string? updaterPackagePath = await _updateService.DownloadUpdaterUpdateAsync(_appUpdateInfo, progress);

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
                    Arguments = $"--src \"{downloadedPath}\" --dst \"{appExe}\" --pid {Environment.ProcessId}" +
                        (string.IsNullOrEmpty(updaterPackagePath) ? string.Empty : $" --updater-src \"{updaterPackagePath}\""),
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
                IsAppUpdateDownloading = false;
            }
        }

        private static string GetAppVersion()
        {
            var version = System.Reflection.Assembly.GetEntryAssembly()?.GetName()?.Version;
            return version?.ToString() ?? "0.0.0";
        }
    }

    /// <summary>
    /// Пункт списка качества аудио: отображаемая подпись и исходное значение для --audio-quality.
    /// </summary>
    public sealed class AudioQualityOption
    {
        public string Display { get; }

        public string Value { get; }

        public AudioQualityOption(string display, string value)
        {
            Display = display;
            Value = value;
        }

        public override string ToString() => Display;
    }
}
