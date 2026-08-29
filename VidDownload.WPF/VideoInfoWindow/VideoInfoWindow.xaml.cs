using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using VidDownload.WPF.Services;
using VidDownload.WPF.ViewModels;

namespace VidDownload.WPF.VideoInfoWindow
{
    public partial class VideoInfoWindow
    {
        public VideoInfoViewModel ViewModel { get; }

        public VideoInfoWindow()
        {
            InitializeComponent();
            ViewModel = AppServices.ServiceProvider.GetRequiredService<VideoInfoViewModel>();
            DataContext = ViewModel;
            UiDialogHost.Attach(this);
        }

        private void OnDownloadClick(object sender, RoutedEventArgs e)
        {
            ViewModel.ConfirmCommand.Execute(null);
            if (ViewModel.Confirmed)
            {
                DialogResult = true;
                Close();
            }
        }

        private void OnCancelClick(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
