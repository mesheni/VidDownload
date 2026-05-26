using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using VidDownload.WPF.Services;
using VidDownload.WPF.ViewModels;

namespace VidDownload.WPF.HistoryWindow
{
    public partial class HistoryWindow : System.Windows.Window
    {
        public string? SelectedUrl { get; private set; }

        public ICommand DownloadAgainCommand { get; }

        public HistoryWindow()
        {
            InitializeComponent();
            DataContext = AppServices.ServiceProvider.GetRequiredService<HistoryViewModel>();

            DownloadAgainCommand = new RelayCommand<DownloadHistoryEntry>(OnDownloadAgain);
        }

        private async void OnDownloadAgain(DownloadHistoryEntry? entry)
        {
            if (entry == null) return;

            SelectedUrl = entry.Url;
            DialogResult = true;
            Close();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private async void OnLoaded(object sender, RoutedEventArgs e)
        {
            if (DataContext is HistoryViewModel vm)
            {
                await vm.LoadAsync();
            }
        }

        private void OnListViewSizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (sender is not ListView listView || listView.View is not GridView gridView || gridView.Columns.Count < 4)
                return;

            var availableWidth = listView.ActualWidth - SystemParameters.VerticalScrollBarWidth - 6;
            var dateWidth = 170.0;
            var statusWidth = 100.0;
            var buttonWidth = 100.0;

            gridView.Columns[0].Width = dateWidth;
            gridView.Columns[2].Width = statusWidth;
            gridView.Columns[3].Width = buttonWidth;

            var remaining = availableWidth - dateWidth - statusWidth - buttonWidth;
            if (remaining > 50)
                gridView.Columns[1].Width = remaining;
        }
    }
}
