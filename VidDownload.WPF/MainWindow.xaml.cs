using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using VidDownload.WPF.Resources;
using VidDownload.WPF.Services;
using VidDownload.WPF.ViewModels;
using Wpf.Ui;
using Wpf.Ui.Controls;

namespace VidDownload.WPF
{
    public partial class MainWindow : FluentWindow
    {
        private readonly MainViewModel _viewModel;
        private bool _forceClose;
        private bool _closeConfirmed;

        public MainWindow()
        {
            InitializeComponent();
            _viewModel = AppServices.ServiceProvider.GetRequiredService<MainViewModel>();
            DataContext = _viewModel;

            // Свои Fluent-диалоги и снекбар отображаются в презентерах этого окна
            var dialogService = AppServices.ServiceProvider.GetRequiredService<IContentDialogService>();
            dialogService.SetDialogHost(RootContentDialog);
            AppServices.ServiceProvider.GetRequiredService<ISnackbarService>()
                .SetSnackbarPresenter(RootSnackbar);

            // Модальные окна при закрытии возвращают хост диалогов сюда
            Closed += (_, _) => dialogService.SetDialogHost(RootContentDialog);
        }

        /// <summary>Хост Fluent-диалогов — используется дочерними окнами для возврата хоста.</summary>
        public Wpf.Ui.Controls.ContentDialogHost DialogHost => RootContentDialog;

        private void OnPasteFromClipboard(object sender, RoutedEventArgs e)
        {
            try
            {
                var text = System.Windows.Clipboard.ContainsText()
                    ? System.Windows.Clipboard.GetText().Trim()
                    : string.Empty;
                if (!string.IsNullOrEmpty(text))
                _viewModel.Url = text;
            }
            catch (Exception ex)
            {
                AppLog.Error(nameof(MainWindow), $"Paste failed: {ex.Message}");
            }
        }

        private void OnWindowDragOver(object sender, DragEventArgs e)
        {
            e.Effects = e.Data.GetDataPresent(DataFormats.FileDrop) || e.Data.GetDataPresent(DataFormats.UnicodeText)
                ? DragDropEffects.Copy
                : DragDropEffects.None;
            e.Handled = true;
        }

        private void OnWindowDrop(object sender, DragEventArgs e)
        {
            // Файл со списком ссылок (.txt)
            if (e.Data.GetDataPresent(DataFormats.FileDrop) &&
                e.Data.GetData(DataFormats.FileDrop) is string[] files)
            {
                var list = files.FirstOrDefault(f =>
                    string.Equals(Path.GetExtension(f), ".txt", StringComparison.OrdinalIgnoreCase));
                if (list != null)
                {
                    _viewModel.ImportUrlsFromFile(list);
                    return;
                }
            }

            // Перетащенный текст со ссылками (одной или несколькими строками)
            if (e.Data.GetDataPresent(DataFormats.UnicodeText) &&
                e.Data.GetData(DataFormats.UnicodeText) is string text)
            {
                _viewModel.ImportUrlsFromText(text);
            }
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
            if (_forceClose || _closeConfirmed)
            {
                base.OnClosing(e);
                return;
            }

            var tray = AppServices.ServiceProvider.GetRequiredService<ITrayService>();

            // Без работающего трея сворачивать окно нельзя — иначе приложение
            // останется без единой точки доступа
            if (_viewModel.MinimizeToTray && tray.IsAvailable)
            {
                e.Cancel = true;
                Hide();
                return;
            }

            if (!_viewModel.HasActiveDownloads)
            {
                base.OnClosing(e);
                return;
            }

            // Подтверждение асинхронное: отменяем закрытие, спрашиваем,
            // затем повторно вызываем Close()
            e.Cancel = true;
            ConfirmExitWithActiveDownloadsAsync();
        }

        private async void ConfirmExitWithActiveDownloadsAsync()
        {
            var loc = LocalizedStrings.Instance;
            bool confirmed = await AppServices.ServiceProvider.GetRequiredService<IDialogService>()
                .AskAsync(loc["ConfirmExitWithDownloads"], loc["ExitConfirmTitle"]);

            if (!confirmed)
                return;

            // Останавливаем процессы yt-dlp до завершения приложения
            AppServices.ServiceProvider.GetRequiredService<IDownloadQueueService>().CancelAll();

            _closeConfirmed = true;
            Close();
        }
    }
}
