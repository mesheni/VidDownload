using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using VidDownload.WPF.Resources;
using VidDownload.WPF.Services;
using VidDownload.WPF.ViewModels.Base;

namespace VidDownload.WPF.ViewModels
{
    /// <summary>
    /// Окно настроек: язык, трей, мониторинг буфера, лимит параллельных загрузок.
    /// Параметры загрузки (папка, лимит скорости) остаются на главном окне —
    /// каждое окно владеет непересекающимся набором полей settings.json.
    /// </summary>
    public partial class SettingsViewModel : ViewModelBase
    {
        /// <summary>Индекс 5 — куки из файла cookies.txt.</summary>
        private const int CookiesFileIndex = 6;

        private static readonly string[] CookiesKeys = { "", "chrome", "edge", "firefox", "opera", "waterfox", "FILE" };

        /// <summary>Схемы прокси, которые принимает yt-dlp (--proxy).</summary>
        private static readonly string[] ProxySchemes = { "http", "https", "socks4", "socks5", "socks5h" };

        private readonly ISettingsService _settingsService;
        private readonly IClipboardMonitorService _clipboardMonitor;
        private readonly IDownloadQueueService _queue;
        private readonly LocalizedStrings _loc;

        public LocalizedStrings LocalizedStrings => _loc;

        public ObservableCollection<string> Languages { get; } = new()
        {
            "RU", "EN", "ZH"
        };

        /// <summary>Локализованные подписи источников куки (пересобираются при смене языка).</summary>
        public ObservableCollection<string> CookiesSources { get; } = new();

        [ObservableProperty]
        private string _selectedLanguage = "RU";

        [ObservableProperty]
        private bool _minimizeToTray;

        [ObservableProperty]
        private bool _clipboardMonitorEnabled;

        [ObservableProperty]
        private int _maxConcurrentDownloads = 1;

        [ObservableProperty]
        private int _selectedCookiesIndex;

        [ObservableProperty]
        private string _cookiesFilePath = string.Empty;

        [ObservableProperty]
        private string _proxy = string.Empty;

        [ObservableProperty]
        private int _retries = 3;

        [ObservableProperty]
        private bool _useDownloadArchive;

        /// <summary>Показывать окно предпросмотра метаданных перед добавлением в очередь.</summary>
        [ObservableProperty]
        private bool _showMetadataPreview = true;

        /// <summary>Текст ошибки формата прокси; пусто — поле валидно.</summary>
        [ObservableProperty]
        private string _proxyError = string.Empty;

        /// <summary>Текст ошибки пути к cookies.txt; пусто — поле валидно.</summary>
        [ObservableProperty]
        private string _cookiesFileError = string.Empty;

        /// <summary>Ошибка сохранения настроек (IO/сериализация); пусто — ошибок нет.</summary>
        [ObservableProperty]
        private string _saveError = string.Empty;

        public bool IsCookiesFileSelected => SelectedCookiesIndex == CookiesFileIndex;

        public bool HasProxyError => !string.IsNullOrEmpty(ProxyError);

        public bool HasCookiesFileError => !string.IsNullOrEmpty(CookiesFileError);

        public SettingsViewModel(
            ISettingsService settingsService,
            IClipboardMonitorService clipboardMonitor,
            IDownloadQueueService queue,
            LocalizedStrings localizedStrings)
        {
            _settingsService = settingsService;
            _clipboardMonitor = clipboardMonitor;
            _queue = queue;
            _loc = localizedStrings;

            _loc.PropertyChanged += OnLocalizedStringsChanged;
            BuildCookiesSources();
        }

        private void OnLocalizedStringsChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (string.IsNullOrEmpty(e.PropertyName))
                BuildCookiesSources();
        }

        private void BuildCookiesSources()
        {
            int selected = SelectedCookiesIndex;
            CookiesSources.Clear();
            CookiesSources.Add(_loc["CookiesNone"]);
            CookiesSources.Add("Chrome");
            CookiesSources.Add("Edge");
            CookiesSources.Add("Firefox");
            CookiesSources.Add("Opera");
            CookiesSources.Add("Waterfox");
            CookiesSources.Add(_loc["CookiesFileOption"]);
            SelectedCookiesIndex = selected;
        }

        partial void OnSelectedCookiesIndexChanged(int value)
        {
            OnPropertyChanged(nameof(IsCookiesFileSelected));
            ValidateCookiesFile();
        }

        partial void OnProxyChanged(string value) => ValidateProxy();

        partial void OnCookiesFilePathChanged(string value) => ValidateCookiesFile();

        partial void OnProxyErrorChanged(string value) => OnPropertyChanged(nameof(HasProxyError));

        partial void OnCookiesFileErrorChanged(string value) => OnPropertyChanged(nameof(HasCookiesFileError));

        /// <summary>
        /// Проверяет все вводимые поля перед сохранением. Ошибки выставляются в
        /// <see cref="ProxyError"/> и <see cref="CookiesFileError"/> для показа под полями.
        /// </summary>
        public bool ValidateSettings()
        {
            ValidateProxy();
            ValidateCookiesFile();
            SaveError = string.Empty;
            return !HasProxyError && !HasCookiesFileError;
        }

        private void ValidateProxy()
        {
            ProxyError = IsValidProxy(Proxy) ? string.Empty : _loc["ProxyInvalid"];
        }

        private void ValidateCookiesFile()
        {
            CookiesFileError = IsCookiesFileSelected && !IsValidCookiesFile(CookiesFilePath)
                ? _loc["CookiesFileNotFound"]
                : string.Empty;
        }

        /// <summary>
        /// Пустая строка допустима (прокси не используется). Иначе ожидается
        /// scheme://host[:port] с одной из поддерживаемых yt-dlp схем.
        /// </summary>
        internal static bool IsValidProxy(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return true;

            if (!Uri.TryCreate(value.Trim(), UriKind.Absolute, out var uri))
                return false;

            return !string.IsNullOrWhiteSpace(uri.Host)
                && ProxySchemes.Contains(uri.Scheme, StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Пустой путь допустим (файл cookies не выбран). Непустой обязан существовать.
        /// </summary>
        internal static bool IsValidCookiesFile(string? path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return true;

            try
            {
                return File.Exists(path.Trim());
            }
            catch (Exception)
            {
                return false;
            }
        }

        public async Task LoadAsync()
        {
            var settings = await _settingsService.LoadAsync().ConfigureAwait(true);

            string language = (settings.Language ?? string.Empty).ToUpper();
            SelectedLanguage = Languages.Contains(language) ? language : "RU";
            MinimizeToTray = settings.MinimizeToTray;
            ClipboardMonitorEnabled = settings.ClipboardMonitorEnabled;
            MaxConcurrentDownloads = Math.Clamp(
                settings.MaxConcurrentDownloads <= 0 ? 1 : settings.MaxConcurrentDownloads, 1, 3);
            Proxy = settings.Proxy ?? string.Empty;
            CookiesFilePath = settings.CookiesFile ?? string.Empty;
            Retries = Math.Clamp(settings.Retries < 0 ? 0 : settings.Retries, 0, 20);
            UseDownloadArchive = settings.UseDownloadArchive;
            ShowMetadataPreview = settings.ShowMetadataPreview;

            if (!string.IsNullOrEmpty(settings.CookiesFile))
                SelectedCookiesIndex = CookiesFileIndex;
            else
                SelectedCookiesIndex = Math.Max(0, Array.IndexOf(CookiesKeys, (settings.CookiesFromBrowser ?? string.Empty).ToLower()));
        }

        [RelayCommand]
        private async Task SaveAsync()
        {
            if (!ValidateSettings())
                return;

            int clamped = Math.Clamp(MaxConcurrentDownloads <= 0 ? 1 : MaxConcurrentDownloads, 1, 3);

            var settings = await _settingsService.LoadAsync().ConfigureAwait(true);
            settings.Language = SelectedLanguage;
            settings.MinimizeToTray = MinimizeToTray;
            settings.ClipboardMonitorEnabled = ClipboardMonitorEnabled;
            settings.MaxConcurrentDownloads = clamped;
            settings.Proxy = Proxy?.Trim() ?? string.Empty;
            settings.Retries = Math.Clamp(Retries < 0 ? 0 : Retries, 0, 20);
            settings.UseDownloadArchive = UseDownloadArchive;
            settings.ShowMetadataPreview = ShowMetadataPreview;

            if (SelectedCookiesIndex == CookiesFileIndex)
            {
                settings.CookiesFromBrowser = string.Empty;
                settings.CookiesFile = CookiesFilePath?.Trim() ?? string.Empty;
            }
            else
            {
                settings.CookiesFromBrowser = CookiesKeys[Math.Clamp(SelectedCookiesIndex, 0, CookiesFileIndex)];
                settings.CookiesFile = string.Empty;
            }

            await _settingsService.SaveAsync(settings).ConfigureAwait(true);

            // Применяем немедленно, без перезапуска
            if (_loc.CurrentLanguage != SelectedLanguage)
                _loc.SetLanguage(SelectedLanguage.ToLower());
            _clipboardMonitor.IsEnabled = ClipboardMonitorEnabled;
            _queue.MaxConcurrent = clamped;
        }

        [RelayCommand]
        private void BrowseCookiesFile()
        {
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "cookies.txt|*.txt|" + _loc["AllFilesFilter"],
                Title = _loc["CookiesFileDialogTitle"]
            };

            if (dialog.ShowDialog() == true)
                CookiesFilePath = dialog.FileName;
        }

        [RelayCommand]
        private void OpenLogsFolder()
        {
            try
            {
                Process.Start("explorer.exe", $"\"{AppPaths.LogsDir}\"");
            }
            catch (Exception ex)
            {
                AppLog.Error(nameof(SettingsViewModel), $"OpenLogsFolder failed: {ex.Message}");
            }
        }
    }
}
