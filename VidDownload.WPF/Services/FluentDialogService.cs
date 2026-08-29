using System;
using System.Threading.Tasks;
using System.Windows;
using VidDownload.WPF.Resources;
using Wpf.Ui;
using Wpf.Ui.Controls;
using Wpf.Ui.Extensions;

namespace VidDownload.WPF.Services
{
    /// <summary>
    /// Диалоги подтверждения на базе Fluent ContentDialog (WPF-UI).
    /// Асинхронные по природе: вызывающие код обязаны использовать await.
    /// </summary>
    public class FluentDialogService : IDialogService
    {
        protected readonly IContentDialogService Dialogs;
        protected readonly ILocalizationService Loc;

        public FluentDialogService(IContentDialogService dialogs, ILocalizationService loc)
        {
            Dialogs = dialogs;
            Loc = loc;
        }

        public virtual Task<bool> AskAsync(string message, string title) => ConfirmAsync(message, title);

        public async Task<bool> ConfirmAsync(string message, string title)
        {
            try
            {
                var result = await Dialogs.ShowSimpleDialogAsync(CreateOptions(title, message));
                return result == ContentDialogResult.Primary;
            }
            catch (Exception ex)
            {
                // Хост диалога не привязан или окно закрыто — не роняем вызывающий поток
                AppLog.Error(nameof(FluentDialogService), $"Dialog failed, fallback to classic: {ex.Message}");
                return System.Windows.MessageBox.Show(
                    message, title, System.Windows.MessageBoxButton.YesNo,
                    System.Windows.MessageBoxImage.Question) == System.Windows.MessageBoxResult.Yes;
            }
        }

        protected SimpleContentDialogCreateOptions CreateOptions(string title, string message) => new()
        {
            Title = title,
            Content = message,
            PrimaryButtonText = Loc.GetString("YesButton"),
            CloseButtonText = Loc.GetString("NoButton"),
            DefaultButton = ContentDialogButton.Primary
        };
    }
}
