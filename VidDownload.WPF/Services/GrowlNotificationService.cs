using System;
using System.Windows;
using HandyControl.Controls;

namespace VidDownload.WPF.Services
{
    /// <summary>
    /// Уведомления через HandyControl Growl (без регистрации контейнера показываются
    /// в отдельном всплывающем окне поверх приложения). Когда главное окно скрыто
    /// в трей, дублирует сообщение ballooon-уведомлением.
    /// </summary>
    public class GrowlNotificationService : INotificationService
    {
        private readonly Lazy<ITrayService> _tray;

        public GrowlNotificationService(Lazy<ITrayService> tray)
        {
            _tray = tray;
        }

        public void Success(string message, string title = "")
        {
            Growl.Success(Compose(title, message));
            NotifyTray(title, message);
        }

        public void Info(string message, string title = "")
        {
            Growl.Info(Compose(title, message));
            NotifyTray(title, message);
        }

        public void Error(string message, string title = "")
        {
            Growl.Error(Compose(title, message));
            NotifyTray(title, message);
        }

        public void Ask(string message, string title, Action onConfirmed)
        {
            Growl.Ask(Compose(title, message), confirmed =>
            {
                if (confirmed)
                    onConfirmed();
                return true;
            });
        }

        private static string Compose(string title, string message) =>
            string.IsNullOrEmpty(title) ? message : $"{title}{Environment.NewLine}{message}";

        private void NotifyTray(string title, string message)
        {
            var mainWindow = Application.Current?.MainWindow;
            if (mainWindow != null && !mainWindow.IsVisible)
                _tray.Value.ShowBalloon(string.IsNullOrEmpty(title) ? "VidDownload" : title, message);
        }
    }
}
