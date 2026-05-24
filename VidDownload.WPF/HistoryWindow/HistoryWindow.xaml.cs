using System.Windows;
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
    }
}
