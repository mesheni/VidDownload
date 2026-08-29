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
        private readonly LocalizedStrings _loc;

        private readonly ObservableCollection<DownloadHistoryEntry> _allEntries = new();
        private int _loadedCount = PageSize;

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

        public HistoryViewModel(
            IDownloadHistoryService historyService,
            INotificationService notifications,
            LocalizedStrings localizedStrings)
        {
            _historyService = historyService;
            _notifications = notifications;
            _loc = localizedStrings;

            EntriesView = CollectionViewSource.GetDefaultView(_allEntries);
            EntriesView.Filter = FilterEntry;
        }

        partial void OnSearchTextChanged(string value) => EntriesView.Refresh();

        private bool FilterEntry(object obj)
        {
            if (string.IsNullOrWhiteSpace(SearchText))
                return true;

            if (obj is not DownloadHistoryEntry entry)
                return true;

            return Contains(entry.Title) || Contains(entry.Url) || Contains(entry.Status.ToString());

            bool Contains(string? field) =>
                !string.IsNullOrEmpty(field) &&
                field.IndexOf(SearchText, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        public async Task LoadAsync()
        {
            var entries = await _historyService.GetRecentEntriesAsync(_loadedCount).ConfigureAwait(true);
            _allEntries.Clear();
            foreach (var entry in entries)
                _allEntries.Add(entry);
            EntriesView.Refresh();
            CanLoadMore = entries.Count >= _loadedCount;
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
            await _historyService.ClearHistoryAsync();
            _allEntries.Clear();
        }

        [RelayCommand]
        private async Task RemoveEntryAsync(DownloadHistoryEntry? entry)
        {
            if (entry == null) return;
            await _historyService.RemoveEntryAsync(entry.Id);
            _allEntries.Remove(entry);
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
