using System;

namespace VidDownload.WPF.Services
{
    public interface INotificationService
    {
        void Success(string message, string title = "");
        void Info(string message, string title = "");
        void Error(string message, string title = "");

        /// <summary>Вопрос с кнопками подтверждения; <paramref name="onConfirmed"/> вызывается при «Да».</summary>
        void Ask(string message, string title, Action onConfirmed);
    }
}
