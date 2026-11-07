using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
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

        /// <summary>
        /// Esc закрывает окно только когда конвертация не идёт.
        /// Во время работы Esc ничего не делает: молча отменять процесс недопустимо.
        /// </summary>
        private void OnPreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Escape)
                return;

            if (DataContext is ConvertViewModel { IsConverting: true })
            {
                e.Handled = true;
                return;
            }

            // Esc в открытом выпадающем списке должен закрывать список, а не окно
            if (Keyboard.FocusedElement is ComboBox { IsDropDownOpen: true })
                return;

            Close();
        }

        private void OnWindowDragOver(object sender, DragEventArgs e)
        {
            // принимаем только реальные файлы: папки и прочие элементы подсвечиваются как недопустимые
            e.Effects = GetExistingFiles(e).Count > 0
                ? DragDropEffects.Copy
                : DragDropEffects.None;
            e.Handled = true;
        }

        private void OnWindowDrop(object sender, DragEventArgs e)
        {
            if (DataContext is not ConvertViewModel vm)
                return;

            var existing = GetExistingFiles(e);
            if (existing.Count == 0)
            {
                // папки и неподдерживаемые элементы не принимаем — сообщаем статусом без модального диалога
                if (e.Data.GetDataPresent(DataFormats.FileDrop))
                    vm.StatusMessage = vm.LocalizedStrings["ConvertDropIgnored"];
                return;
            }

            if (vm.IsBatchMode)
                vm.AddBatchFiles(existing);
            else
                vm.FilePath = existing[0];
        }

        /// <summary>Существующие файлы из перетаскиваемого набора (папки отбрасываются).</summary>
        private static List<string> GetExistingFiles(DragEventArgs e)
        {
            if (!e.Data.GetDataPresent(DataFormats.FileDrop) ||
                e.Data.GetData(DataFormats.FileDrop) is not string[] files)
                return new List<string>();

            return files.Where(File.Exists).ToList();
        }
    }
}
