using System;
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
            // Не закрываем окно молча при невалидных полях: подсвечиваем ошибку
            // под полем и переводим туда фокус.
            if (!_viewModel.ValidateSettings())
            {
                if (_viewModel.HasProxyError)
                    ProxyBox.Focus();
                else if (_viewModel.HasCookiesFileError)
                    CookiesFileBox.Focus();
                return;
            }

            try
            {
                await _viewModel.SaveCommand.ExecuteAsync(null);
                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                AppLog.Error(nameof(SettingsWindow), $"Save failed: {ex.Message}");
                _viewModel.SaveError = _viewModel.LocalizedStrings["SettingsSaveFailed"];
            }
        }
    }
}
