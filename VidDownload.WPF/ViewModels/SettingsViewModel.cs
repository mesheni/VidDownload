using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
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
        private readonly ISettingsService _settingsService;
        private readonly IClipboardMonitorService _clipboardMonitor;
        private readonly IDownloadQueueService _queue;
        private readonly LocalizedStrings _loc;

        public LocalizedStrings LocalizedStrings => _loc;

        public ObservableCollection<string> Languages { get; } = new()
        {
            "RU", "EN", "ZH"
        };

        [ObservableProperty]
        private string _selectedLanguage = "RU";

        [ObservableProperty]
        private bool _minimizeToTray;

        [ObservableProperty]
        private bool _clipboardMonitorEnabled;

        [ObservableProperty]
        private int _maxConcurrentDownloads = 1;

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
        }

        [RelayCommand]
        private async Task SaveAsync()
        {
            int clamped = Math.Clamp(MaxConcurrentDownloads <= 0 ? 1 : MaxConcurrentDownloads, 1, 3);

            var settings = await _settingsService.LoadAsync().ConfigureAwait(true);
            settings.Language = SelectedLanguage;
            settings.MinimizeToTray = MinimizeToTray;
            settings.ClipboardMonitorEnabled = ClipboardMonitorEnabled;
            settings.MaxConcurrentDownloads = clamped;
            await _settingsService.SaveAsync(settings).ConfigureAwait(true);

            // Применяем немедленно, без перезапуска
            if (_loc.CurrentLanguage != SelectedLanguage)
                _loc.SetLanguage(SelectedLanguage.ToLower());
            _clipboardMonitor.IsEnabled = ClipboardMonitorEnabled;
            _queue.MaxConcurrent = clamped;
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
