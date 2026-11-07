using System;
using System.Linq;
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
    /// Асинхронные по природе: вызывающие код обязан использовать await.
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
                return ShowOwnedMessageBox(
                    message, title, System.Windows.MessageBoxButton.YesNo, System.Windows.MessageBoxImage.Question)
                    == System.Windows.MessageBoxResult.Yes;
            }
        }

        protected SimpleContentDialogCreateOptions CreateOptions(string title, string message) => new()
        {
            Title = title,
            Content = message,
            PrimaryButtonText = Loc.GetString("YesButton"),
            CloseButtonText = Loc.GetString("NoButton"),
            // Безопасный дефолт: Enter не подтверждает потенциально опасное действие
            DefaultButton = ContentDialogButton.Close
        };

        /// <summary>Классический MessageBox с владельцем — чтобы он не терялся за модальным окном.</summary>
        protected static System.Windows.MessageBoxResult ShowOwnedMessageBox(
            string message, string title, System.Windows.MessageBoxButton buttons, System.Windows.MessageBoxImage image)
        {
            // Владельцем берём активное окно: иначе fallback может оказаться под открытым модальным окном
            var owner = System.Windows.Application.Current?.Windows
                            .OfType<System.Windows.Window>()
                            .FirstOrDefault(w => w.IsActive && w.IsVisible)
                        ?? System.Windows.Application.Current?.MainWindow;
            return owner is not null
                ? System.Windows.MessageBox.Show(owner, message, title, buttons, image)
                : System.Windows.MessageBox.Show(message, title, buttons, image);
        }
    }
}
