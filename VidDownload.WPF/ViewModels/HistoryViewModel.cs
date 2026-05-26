using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using VidDownload.WPF.Services;
using VidDownload.WPF.ViewModels.Base;

namespace VidDownload.WPF.ViewModels
{
    public partial class HistoryViewModel : ViewModelBase
    {
        private readonly IDownloadHistoryService _historyService;

        [ObservableProperty]
        private ObservableCollection<DownloadHistoryEntry> _entries = new();

        [ObservableProperty]
        private DownloadHistoryEntry? _selectedEntry;

        public HistoryViewModel(IDownloadHistoryService historyService)
        {
            _historyService = historyService;
        }

        public async Task LoadAsync()
        {
            var entries = await _historyService.GetRecentEntriesAsync().ConfigureAwait(true);
            Entries = new ObservableCollection<DownloadHistoryEntry>(entries);
        }

        [RelayCommand]
        private async Task ClearHistoryAsync()
        {
            await _historyService.ClearHistoryAsync();
            Entries.Clear();
        }

        [RelayCommand]
        private async Task RemoveEntryAsync(DownloadHistoryEntry? entry)
        {
            if (entry == null) return;
            await _historyService.RemoveEntryAsync(entry.Id);
            Entries.Remove(entry);
        }
    }
}
