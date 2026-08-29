using System;
using System.Threading.Tasks;
using System.Windows;
using Wpf.Ui;
using Wpf.Ui.Controls;
using Wpf.Ui.Extensions;

namespace VidDownload.WPF.Services
{
    /// <summary>
    /// Уведомления через Fluent Snackbar справа внизу главного окна (WPF-UI).
    /// Когда главное окно скрыто в трей — дублирует balloon-уведомлением,
    /// а вопрос с подтверждением сначала разворачивает окно.
    /// </summary>
    public class SnackbarNotificationService : INotificationService
    {
        private static readonly TimeSpan NotificationTimeout = TimeSpan.FromSeconds(4);

        private readonly ISnackbarService _snackbar;
        private readonly Lazy<ITrayService> _tray;
        private readonly IContentDialogService _dialogs;
        private readonly ILocalizationService _loc;

        public SnackbarNotificationService(
            ISnackbarService snackbar,
            IContentDialogService dialogs,
            ILocalizationService loc,
            Lazy<ITrayService> tray)
        {
            _snackbar = snackbar;
            _dialogs = dialogs;
            _loc = loc;
            _tray = tray;
        }

        public void Success(string message, string title = "")
            => Show(title, message, ControlAppearance.Success, SymbolRegular.CheckmarkCircle24);

        public void Info(string message, string title = "")
            => Show(title, message, ControlAppearance.Info, SymbolRegular.Info24);

        public void Error(string message, string title = "")
            => Show(title, message, ControlAppearance.Danger, SymbolRegular.ErrorCircle24);

        public async void Ask(string message, string title, Action onConfirmed)
        {
            EnsureMainWindowVisible();

            try
            {
                var result = await _dialogs.ShowSimpleDialogAsync(new SimpleContentDialogCreateOptions
                {
                    Title = title,
                    Content = message,
                    PrimaryButtonText = _loc.GetString("YesButton"),
                    CloseButtonText = _loc.GetString("NoButton"),
                    DefaultButton = ContentDialogButton.Primary
                });

                if (result == ContentDialogResult.Primary)
                    onConfirmed();
            }
            catch (Exception ex)
            {
                AppLog.Error(nameof(SnackbarNotificationService), $"Ask dialog failed: {ex.Message}");
            }
        }

        private void Show(string title, string message, ControlAppearance appearance, SymbolRegular icon)
        {
            try
            {
                _snackbar.Show(
                    string.IsNullOrEmpty(title) ? "VidDownload" : title,
                    message,
                    appearance,
                    new SymbolIcon(icon),
                    NotificationTimeout);
            }
            catch (Exception ex)
            {
                // Снекбар-хост не привязан (окно закрыто) — логируем без падения потока загрузки
                AppLog.Error(nameof(SnackbarNotificationService), $"Snackbar failed: {ex.Message}");
            }

            NotifyTray(title, message);
        }

        private void NotifyTray(string title, string message)
        {
            var mainWindow = Application.Current?.MainWindow;
            if (mainWindow != null && !mainWindow.IsVisible)
                _tray.Value.ShowBalloon(string.IsNullOrEmpty(title) ? "VidDownload" : title, message);
        }

        private static void EnsureMainWindowVisible()
        {
            var mainWindow = Application.Current?.MainWindow;
            if (mainWindow == null || mainWindow.IsVisible)
                return;

            mainWindow.Show();
            if (mainWindow.WindowState == WindowState.Minimized)
                mainWindow.WindowState = WindowState.Normal;
            mainWindow.Activate();
        }
    }
}
