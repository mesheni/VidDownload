using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using VidDownload.WPF.Services;
using VidDownload.WPF.ViewModels;

namespace VidDownload.WPF.HistoryWindow
{
    public partial class HistoryWindow
    {
        public string? SelectedUrl { get; private set; }

        public ICommand DownloadAgainCommand { get; }

        public HistoryWindow()
        {
            InitializeComponent();
            DataContext = AppServices.ServiceProvider.GetRequiredService<HistoryViewModel>();
            UiDialogHost.Attach(this);

            DownloadAgainCommand = new RelayCommand<DownloadHistoryEntry>(OnDownloadAgain);
        }

        private void OnDownloadAgain(DownloadHistoryEntry? entry)
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
            // «Скачать снова» (~120) + три ghost-кнопки 32x32 с отступами (~108) + запас.
            var actionsWidth = 240.0;

            gridView.Columns[0].Width = dateWidth;
            gridView.Columns[2].Width = statusWidth;
            gridView.Columns[3].Width = actionsWidth;

            var remaining = availableWidth - dateWidth - statusWidth - actionsWidth;
            if (remaining > 50)
                gridView.Columns[1].Width = remaining;
        }

        /// <summary>Двойной клик по строке — повторное скачивание записи.</summary>
        private void OnRowDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (sender is not ListView listView || e.OriginalSource is not DependencyObject source)
                return;

            // Клики по кнопкам действий не должны срабатывать как двойной клик по строке.
            if (IsInsideButton(source))
                return;

            var item = ItemsControl.ContainerFromElement(listView, source) as ListViewItem;
            if (item?.DataContext is DownloadHistoryEntry entry)
                DownloadAgainCommand.Execute(entry);
        }

        private static bool IsInsideButton(DependencyObject source)
        {
            for (DependencyObject? current = source; current != null;)
            {
                if (current is ButtonBase)
                    return true;

                current = current is Visual or System.Windows.Media.Media3D.Visual3D
                    ? VisualTreeHelper.GetParent(current)
                    : LogicalTreeHelper.GetParent(current);
            }

            return false;
        }

        /// <summary>Перед открытием контекстного меню выделяем строку под курсором.</summary>
        private void OnListViewContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            if (sender is not ListView listView || e.OriginalSource is not DependencyObject source)
            {
                e.Handled = true;
                return;
            }

            var item = ItemsControl.ContainerFromElement(listView, source) as ListViewItem;
            if (item?.DataContext is not DownloadHistoryEntry)
            {
                e.Handled = true;
                return;
            }

            listView.SelectedItem = item.DataContext;
        }
    }
}
