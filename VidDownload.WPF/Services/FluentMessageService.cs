using System;
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
                System.Windows.MessageBox.Show(message, title, System.Windows.MessageBoxButton.OK, image);
            }
        }
    }
}
