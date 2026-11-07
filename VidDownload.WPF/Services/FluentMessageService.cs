using System;
using System.Linq;
using System.Windows;
using Wpf.Ui;
using Wpf.Ui.Controls;
using Wpf.Ui.Extensions;

namespace VidDownload.WPF.Services
{
    /// <summary>
    /// Информационные сообщения на базе Fluent ContentDialog (WPF-UI).
    /// Методы неблокирующие: диалог открывается асинхронно.
    /// </summary>
    public class FluentMessageService : IMessageService
    {
        private readonly IContentDialogService _dialogs;
        private readonly ILocalizationService _loc;

        public FluentMessageService(IContentDialogService dialogs, ILocalizationService loc)
        {
            _dialogs = dialogs;
            _loc = loc;
        }

        public void Info(string message, string title)
            => ShowAsync(title, message, MessageBoxImage.Information);

        public void Warning(string message, string title)
            => ShowAsync(title, message, MessageBoxImage.Warning);

        public void Error(string message, string title)
            => ShowAsync(title, message, MessageBoxImage.Error);

        private async void ShowAsync(string title, string message, MessageBoxImage image)
        {
            try
            {
                // SimpleContentDialogCreateOptions не поддерживает иконку/severity,
                // поэтому Fluent-диалог показывает только заголовок и текст.
                await _dialogs.ShowSimpleDialogAsync(new SimpleContentDialogCreateOptions
                {
                    Title = title,
                    Content = message,
                    CloseButtonText = _loc.GetString("OkButton"),
                    DefaultButton = ContentDialogButton.Close
                });
            }
            catch (Exception ex)
            {
                AppLog.Error(nameof(FluentMessageService), $"Dialog failed, fallback to classic: {ex.Message}");
                // Владельцем берём активное окно: иначе fallback может оказаться под открытым модальным окном
                var owner = Application.Current?.Windows
                                .OfType<Window>()
                                .FirstOrDefault(w => w.IsActive && w.IsVisible)
                            ?? Application.Current?.MainWindow;
                if (owner is not null)
                    System.Windows.MessageBox.Show(owner, message, title, System.Windows.MessageBoxButton.OK, image);
                else
                    System.Windows.MessageBox.Show(message, title, System.Windows.MessageBoxButton.OK, image);
            }
        }
    }
}
