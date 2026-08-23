using System.ComponentModel;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using VidDownload.WPF.Resources;
using VidDownload.WPF.Services;
using VidDownload.WPF.ViewModels;

namespace VidDownload.WPF
{
    public partial class MainWindow : Window
    {
        private readonly MainViewModel _viewModel;
        private bool _forceClose;

        public MainWindow()
        {
            InitializeComponent();
            _viewModel = AppServices.ServiceProvider.GetRequiredService<MainViewModel>();
            DataContext = _viewModel;
        }

        /// <summary>Закрывает окно и завершает приложение без вопросов (из трея/обновления).</summary>
        public void ForceExit()
        {
            _forceClose = true;
            Close();
            Application.Current.Shutdown();
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            var loc = LocalizedStrings.Instance;

            // Без работающего трея сворачивать окно нельзя — иначе приложение
            // останется без единой точки доступа
            var tray = AppServices.ServiceProvider.GetRequiredService<ITrayService>();
            if (!_forceClose && _viewModel.MinimizeToTray && tray.IsAvailable)
            {
                // Сворачиваем в трей вместо выхода
                e.Cancel = true;
                Hide();
                return;
            }

            if (!_forceClose && _viewModel.HasActiveDownloads)
            {
                var result = HandyControl.Controls.MessageBox.Show(
                    loc["ConfirmExitWithDownloads"],
                    loc["ExitConfirmTitle"],
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result != MessageBoxResult.Yes)
                {
                    e.Cancel = true;
                    return;
                }

                // Останавливаем процессы yt-dlp до завершения приложения
                AppServices.ServiceProvider.GetRequiredService<IDownloadQueueService>().CancelAll();
            }

            HandyControl.Controls.Growl.Clear();
            base.OnClosing(e);
        }
    }
}
