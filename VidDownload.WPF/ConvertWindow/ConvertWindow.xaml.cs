using System.IO;
using System.Linq;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using VidDownload.WPF.Services;
using VidDownload.WPF.ViewModels;

namespace VidDownload.WPF.ConvertWindow
{
    public partial class ConvertWindow
    {
        public ConvertWindow()
        {
            InitializeComponent();
            DataContext = AppServices.ServiceProvider.GetRequiredService<ConvertViewModel>();
            UiDialogHost.Attach(this);
            Loaded += ConvertWindow_Loaded;
        }

        private async void ConvertWindow_Loaded(object sender, RoutedEventArgs e)
        {
            if (DataContext is ConvertViewModel vm)
            {
                await vm.InitializeAsync();
            }
        }

        private void OnWindowDragOver(object sender, DragEventArgs e)
        {
            e.Effects = e.Data.GetDataPresent(DataFormats.FileDrop)
                ? DragDropEffects.Copy
                : DragDropEffects.None;
            e.Handled = true;
        }

        private void OnWindowDrop(object sender, DragEventArgs e)
        {
            if (!e.Data.GetDataPresent(DataFormats.FileDrop) ||
                e.Data.GetData(DataFormats.FileDrop) is not string[] files)
                return;

            var existing = files.Where(File.Exists).ToList();
            if (existing.Count == 0)
                return;

            if (DataContext is ConvertViewModel vm)
            {
                if (vm.IsBatchMode)
                    vm.AddBatchFiles(existing);
                else
                    vm.FilePath = existing[0];
            }
        }
    }
}
