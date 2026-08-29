using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using VidDownload.WPF.Services;
using VidDownload.WPF.ViewModels;

namespace VidDownload.WPF.SettingsWindow
{
    public partial class SettingsWindow
    {
        private readonly SettingsViewModel _viewModel;

        public SettingsWindow()
        {
            InitializeComponent();
            _viewModel = AppServices.ServiceProvider.GetRequiredService<SettingsViewModel>();
            DataContext = _viewModel;
            UiDialogHost.Attach(this);
            Loaded += OnLoaded;
        }

        private async void OnLoaded(object sender, RoutedEventArgs e)
        {
            await _viewModel.LoadAsync();
        }

        private async void OnSaveClick(object sender, RoutedEventArgs e)
        {
            await _viewModel.SaveCommand.ExecuteAsync(null);
            DialogResult = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
