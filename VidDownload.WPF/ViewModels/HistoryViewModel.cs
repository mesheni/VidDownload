using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using VidDownload.WPF.Converters;
using VidDownload.WPF.Resources;
using VidDownload.WPF.Services;
using VidDownload.WPF.ViewModels.Base;

namespace VidDownload.WPF.ViewModels
{
    public partial class HistoryViewModel : ViewModelBase
    {
        private const int PageSize = 50;

        private readonly IDownloadHistoryService _historyService;
        private readonly INotificationService _notifications;
        private readonly IDialogService _dialogService;
        private readonly LocalizedStrings _loc;

        private readonly ObservableCollection<DownloadHistoryEntry> _allEntries = new();
        private int _loadedCount = PageSize;
        private bool _loaded;

        public LocalizedStrings LocalizedStrings => _loc;

        /// <summary>Отфильтрованное представление истории для таблицы.</summary>
        public ICollectionView EntriesView { get; }

        [ObservableProperty]
        private string _searchText = string.Empty;

        [ObservableProperty]
        private DownloadHistoryEntry? _selectedEntry;

        /// <summary>Возможно ли догрузить более старые записи («Показать ещё»).</summary>
        [ObservableProperty]
        private bool _canLoadMore;

        /// <summary>История полностью пуста (ни одной записи).</summary>
        [ObservableProperty]
        private bool _isEmpty;

        /// <summary>Записи есть, но поиск не дал результатов.</summary>
        [ObservableProperty]
        private bool _hasNoResults;

        public HistoryViewModel(
            IDownloadHistoryService historyService,
            INotificationService notifications,
            IDialogService dialogService,
            LocalizedStrings localizedStrings)
        {
            _historyService = historyService;
            _notifications = notifications;
            _dialogService = dialogService;
            _loc = localizedStrings;

            EntriesView = CollectionViewSource.GetDefaultView(_allEntries);
            EntriesView.Filter = FilterEntry;
        }

        partial void OnSearchTextChanged(string value)
        {
            EntriesView.Refresh();
            UpdateEmptyStates();
        }

        private bool FilterEntry(object obj)
        {
            if (string.IsNullOrWhiteSpace(SearchText))
                return true;

            if (obj is not DownloadHistoryEntry entry)
                return true;

            string statusText = _loc[DownloadStatusToTextConverter.GetResourceKey(entry.Status)];
            return MatchesSearch(entry.Title, entry.Url, statusText, SearchText);
        }

        /// <summary>Поиск по названию, ссылке и ЛОКАЛИЗОВАННОМУ статусу (чистая логика для тестов).</summary>
        internal static bool MatchesSearch(string? title, string? url, string? statusText, string? query)
        {
            if (string.IsNullOrWhiteSpace(query))
                return true;

            return Contains(title) || Contains(url) || Contains(statusText);

            bool Contains(string? field) =>
                !string.IsNullOrEmpty(field) &&
                field.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        /// <summary>Есть ли ещё записи за пределами уже показанной страницы.</summary>
        internal static bool HasMoreEntries(int fetchedCount, int displayLimit) => fetchedCount > displayLimit;

        private void UpdateEmptyStates()
        {
            IsEmpty = _loaded && _allEntries.Count == 0;
            HasNoResults = _loaded && _allEntries.Count > 0 && EntriesView.IsEmpty;
        }

        public async Task LoadAsync()
        {
            // Запрашиваем на одну запись больше, чтобы честно понять, есть ли следующая страница.
            var fetched = await _historyService.GetRecentEntriesAsync(_loadedCount + 1).ConfigureAwait(true);
            CanLoadMore = HasMoreEntries(fetched.Count, _loadedCount);

            _allEntries.Clear();
            foreach (var entry in fetched.Take(_loadedCount))
                _allEntries.Add(entry);

            _loaded = true;
            EntriesView.Refresh();
            UpdateEmptyStates();
        }

        [RelayCommand]
        private async Task LoadMoreAsync()
        {
            _loadedCount += PageSize;
            await LoadAsync();
        }

        [RelayCommand]
        private async Task ExportCsvAsync()
        {
            var dialog = new SaveFileDialog
            {
                Filter = _loc["CsvFileFilter"],
                FileName = $"VidDownload-history-{DateTime.Now:yyyy-MM-dd}.csv"
            };

            if (dialog.ShowDialog() != true)
                return;

            try
            {
                var sb = new StringBuilder();
                sb.AppendLine("Date;Title;Url;Status;FilePath");
                foreach (var entry in _allEntries)
                {
                    sb.Append(EscapeCsv(entry.Timestamp.ToString("yyyy-MM-dd HH:mm:ss"))).Append(';');
                    sb.Append(EscapeCsv(entry.Title)).Append(';');
                    sb.Append(EscapeCsv(entry.Url)).Append(';');
                    sb.Append(EscapeCsv(entry.Status.ToString())).Append(';');
                    sb.Append(EscapeCsv(entry.FilePath));
                    sb.AppendLine();
                }

                // UTF-8 с BOM, чтобы кириллица корректно открывалась в Excel
                await File.WriteAllTextAsync(dialog.FileName, sb.ToString(), new UTF8Encoding(true)).ConfigureAwait(true);
                _notifications.Info(string.Format(_loc["HistoryExportDone"], dialog.FileName));
            }
            catch (Exception ex)
            {
                _notifications.Error(string.Format(_loc["ErrorWithMessage"], ex.Message));
            }
        }

        private static string EscapeCsv(string? value)
        {
            value ??= string.Empty;
            return value.Contains(';') || value.Contains('"') || value.Contains('\n')
                ? "\"" + value.Replace("\"", "\"\"") + "\""
                : value;
        }

        [RelayCommand]
        private async Task ClearHistoryAsync()
        {
            bool confirmed = await _dialogService
                .ConfirmAsync(_loc["HistoryClearConfirm"], _loc["WarningTitle"])
                .ConfigureAwait(true);
            if (!confirmed)
                return;

            await _historyService.ClearHistoryAsync();
            _allEntries.Clear();
            SelectedEntry = null;
            _loadedCount = PageSize;
            CanLoadMore = false;
            EntriesView.Refresh();
            UpdateEmptyStates();
        }

        [RelayCommand]
        private async Task RemoveEntryAsync(DownloadHistoryEntry? entry)
        {
            if (entry == null) return;
            await _historyService.RemoveEntryAsync(entry.Id);
            _allEntries.Remove(entry);
            if (ReferenceEquals(SelectedEntry, entry))
                SelectedEntry = null;
            EntriesView.Refresh();
            UpdateEmptyStates();
        }

        [RelayCommand]
        private void OpenEntryFile(DownloadHistoryEntry? entry)
        {
            if (entry == null)
                return;

            if (string.IsNullOrEmpty(entry.FilePath) || !File.Exists(entry.FilePath))
            {
                _notifications.Error(string.Format(_loc["HistoryFileMissing"], entry.FilePath ?? entry.Url));
                return;
            }

            Process.Start("explorer.exe", $"/select,\"{entry.FilePath}\"");
        }

        [RelayCommand]
        private void OpenEntryFolder(DownloadHistoryEntry? entry)
        {
            if (entry == null)
                return;

            string? folder = string.IsNullOrEmpty(entry.FilePath)
                ? null
                : Path.GetDirectoryName(entry.FilePath);

            if (string.IsNullOrEmpty(folder) || !Directory.Exists(folder))
            {
                _notifications.Error(string.Format(_loc["HistoryFileMissing"], entry.FilePath ?? entry.Url));
                return;
            }

            Process.Start("explorer.exe", $"\"{folder}\"");
        }
    }
}
