using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
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
        private readonly INotificationService _notifications;
        private bool _forceClose;
        private bool _closeConfirmed;

        public MainWindow()
        {
            InitializeComponent();
            _viewModel = AppServices.ServiceProvider.GetRequiredService<MainViewModel>();
            _notifications = AppServices.ServiceProvider.GetRequiredService<INotificationService>();
            DataContext = _viewModel;

            // Фокус: стартовое поле — ссылка; после вставки и после постановки в очередь
            // возвращаем фокус туда же, чтобы следующее нажатие клавиши не терялось
            Loaded += (_, _) => UrlBox.Focus();
            _viewModel.DownloadEnqueued += (_, _) => UrlBox.Focus();
            _viewModel.FocusUrlRequested += (_, _) => UrlBox.Focus();

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

        /// <summary>
        /// Адаптивные колонки истории: при сужении окна колонка «URL / Название» сжимается,
        /// чтобы кнопки действий не уезжали за правый край (горизонтальная прокрутка отключена).
        /// </summary>
        private void OnHistoryListSizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (sender is not System.Windows.Controls.ListView listView
                || listView.View is not System.Windows.Controls.GridView gridView
                || gridView.Columns.Count < 4)
                return;

            double available = listView.ActualWidth - SystemParameters.VerticalScrollBarWidth - 4;
            if (available <= 0)
                return;

            // Нижние границы: дата не уже 120, действия не уже 200, статус — 90.
            const double minDate = 120;
            const double minStatus = 90;
            const double minActions = 200;
            const double minTitle = 140;

            double dateWidth = 150;
            double statusWidth = 100;
            double actionsWidth = 220;

            double flexible = available - dateWidth - statusWidth - actionsWidth;
            if (flexible < minTitle)
            {
                // Не хватает места — сжимаем фиксированные колонки до минимумов.
                double deficit = minTitle - flexible;
                double shrinkDate = Math.Min(deficit / 2, dateWidth - minDate);
                dateWidth -= shrinkDate;
                deficit -= shrinkDate;
                double shrinkActions = Math.Min(deficit, actionsWidth - minActions);
                actionsWidth -= shrinkActions;
                deficit -= shrinkActions;
                if (deficit > 0)
                    statusWidth = Math.Max(minStatus, statusWidth - deficit);
                flexible = available - dateWidth - statusWidth - actionsWidth;
            }

            gridView.Columns[0].Width = dateWidth;
            gridView.Columns[2].Width = statusWidth;
            gridView.Columns[3].Width = actionsWidth;
            if (flexible > 0)
                gridView.Columns[1].Width = flexible;
        }

        /// <summary>
        /// Esc отменяет активную загрузку, но не перехватывается у открытого диалога
        /// или раскрытого выпадающего списка — они должны закрыться сами.
        /// </summary>
        private void OnWindowKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Escape)
                return;

            // Открытый Fluent ContentDialog (ContentDialogHost — ContentControl) забирает Esc себе
            if (RootContentDialog.Content is not null)
                return;

            if (Keyboard.FocusedElement is ComboBox { IsDropDownOpen: true })
                return;

            if (_viewModel.CancelActiveCommand.CanExecute(null))
            {
                _viewModel.CancelActiveCommand.Execute(null);
                e.Handled = true;
            }
        }

        private void OnPasteFromClipboard(object sender, RoutedEventArgs e)
        {
            try
            {
                var text = System.Windows.Clipboard.ContainsText()
                    ? System.Windows.Clipboard.GetText().Trim()
                    : string.Empty;

                if (string.IsNullOrEmpty(text))
                {
                    // Пустой буфер — это не «сломанная кнопка», а понятное сообщение
                    _notifications.Info(LocalizedStrings.Instance["ClipboardEmpty"]);
                    return;
                }

                _viewModel.Url = text;
                UrlBox.Focus();
                UrlBox.SelectAll();
            }
            catch (Exception ex)
            {
                AppLog.Error(nameof(MainWindow), $"Paste failed: {ex.Message}");
            }
        }

        /// <summary>Перетаскивание принимаем только для текста или .txt-списков.</summary>
        private static bool HasDroppablePayload(IDataObject data)
        {
            if (data.GetDataPresent(DataFormats.UnicodeText))
                return true;

            if (!data.GetDataPresent(DataFormats.FileDrop) || data.GetData(DataFormats.FileDrop) is not string[] files)
                return false;

            return files.Any(f => string.Equals(Path.GetExtension(f), ".txt", StringComparison.OrdinalIgnoreCase));
        }

        private void OnWindowDragOver(object sender, DragEventArgs e)
        {
            if (_viewModel.IsConverterPage)
            {
                e.Effects = (e.Data.GetDataPresent(DataFormats.FileDrop) &&
                             e.Data.GetData(DataFormats.FileDrop) is string[] f &&
                             f.Any(File.Exists))
                    ? DragDropEffects.Copy
                    : DragDropEffects.None;
                e.Handled = true;
                return;
            }

            e.Effects = HasDroppablePayload(e.Data)
                ? DragDropEffects.Copy
                : DragDropEffects.None;
            e.Handled = true;
        }

        private void OnWindowDrop(object sender, DragEventArgs e)
        {
            if (_viewModel.IsConverterPage)
            {
                if (e.Data.GetDataPresent(DataFormats.FileDrop) &&
                    e.Data.GetData(DataFormats.FileDrop) is string[] cFiles)
                {
                    var existing = cFiles.Where(File.Exists).ToList();
                    if (existing.Count > 0)
                    {
                        if (_viewModel.ConvertVm.IsBatchMode)
                            _viewModel.ConvertVm.AddBatchFiles(existing);
                        else
                            _viewModel.ConvertVm.FilePath = existing[0];
                        return;
                    }
                }
            }
            // Файлы со списками ссылок (.txt) — импортируем все, о лишних сообщаем
            if (e.Data.GetDataPresent(DataFormats.FileDrop) &&
                e.Data.GetData(DataFormats.FileDrop) is string[] files)
            {
                var textFiles = files
                    .Where(f => string.Equals(Path.GetExtension(f), ".txt", StringComparison.OrdinalIgnoreCase))
                    .ToList();

                foreach (string file in textFiles)
                    _viewModel.ImportUrlsFromFile(file);

                int ignored = files.Length - textFiles.Count;
                if (ignored > 0)
                    _notifications.Info(string.Format(LocalizedStrings.Instance["DropIgnoredFiles"], ignored));

                if (textFiles.Count > 0)
                    return;
            }

            // Перетащенный текст со ссылками (одной или несколькими строками)
            if (e.Data.GetDataPresent(DataFormats.UnicodeText) &&
                e.Data.GetData(DataFormats.UnicodeText) is string text)
            {
                _viewModel.ImportUrlsFromText(text);
            }
        }

        /// <summary>Колесо мыши не должно менять действие «после очереди» без фокуса на списке.</summary>
        private void OnPostQueueComboPreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (sender is System.Windows.Controls.ComboBox combo && !combo.IsKeyboardFocusWithin)
                e.Handled = true;
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
