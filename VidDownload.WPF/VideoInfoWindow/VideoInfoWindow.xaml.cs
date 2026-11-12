using System;
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

            SizeChanged += OnWindowSizeChanged;
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

        /// <summary>
        /// Выравнивает высоту списка под целое число строк: WPF режет последнюю видимую
        /// строку пополам, если высота вьюпорта не кратна высоте пункта. Вместо среза
        /// остаётся небольшой зазор снизу.
        /// </summary>
        private void OnSnapListSizeChanged(object sender, SizeChangedEventArgs e)
        {
            // Фиксация уже стоит — пересчёт возможен только через изменение окна,
            // иначе сброс и установка одной величины зациклят событие.
            if (sender is System.Windows.Controls.ListBox list && double.IsNaN(list.Height))
                SnapAfterLayout(list);
        }

        private void OnWindowSizeChanged(object sender, SizeChangedEventArgs e)
        {
            SnapAfterLayout(EntriesList);
        }

        /// <summary>Сбрасывает фиксацию и после прохода раскладки заново выравнивает высоту.</summary>
        private void SnapAfterLayout(System.Windows.Controls.ListBox list)
        {
            list.ClearValue(FrameworkElement.HeightProperty);
            Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Loaded, () =>
            {
                double rowHeight = GetRealizedRowHeight(list);
                double available = list.ActualHeight;
                if (rowHeight < 1 || available <= rowHeight)
                    return;

                double snapped = Math.Floor(available / rowHeight + 0.01) * rowHeight;
                if (snapped > 0)
                    list.Height = snapped;
            });
        }

        private static double GetRealizedRowHeight(System.Windows.Controls.ListBox list)
        {
            // При виртуализации контейнер нулевого индекса может быть не создан —
            // ищем первый сгенерированный контейнер.
            for (int i = 0; i < list.Items.Count; i++)
            {
                if (list.ItemContainerGenerator.ContainerFromIndex(i) is System.Windows.Controls.ListBoxItem item
                    && item.ActualHeight > 0)
                {
                    return item.ActualHeight;
                }
            }

            return 0;
        }
    }
}
