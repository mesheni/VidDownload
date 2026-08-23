using System;

namespace VidDownload.WPF.Services
{
    public interface ITrayService : IDisposable
    {
        /// <summary>Создаёт иконку в области уведомлений. Вызывать один раз на UI-потоке.</summary>
        void Initialize();

        /// <summary>Трей успешно создан. Если false, сворачивание в трей недоступно.</summary>
        bool IsAvailable { get; }

        /// <summary>Показывает balloon-уведомление из трея.</summary>
        void ShowBalloon(string title, string message);

        event EventHandler? ShowRequested;
        event EventHandler? OpenDownloadsRequested;
        event EventHandler? ExitRequested;
    }
}
