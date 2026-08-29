using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Wpf.Ui;

namespace VidDownload.WPF.Services
{
    /// <summary>
    /// Привязывает ContentDialogService к презентеру конкретного окна.
    /// После закрытия модального окна возвращает хост главному окну.
    /// </summary>
    public static class UiDialogHost
    {
        public static void Attach(Window window)
        {
            var service = AppServices.ServiceProvider.GetRequiredService<IContentDialogService>();

            if (window.FindName("RootContentDialog") is Wpf.Ui.Controls.ContentDialogHost host)
                service.SetDialogHost(host);
            else
                AppLog.Error(nameof(UiDialogHost), $"RootContentDialog not found in {window.GetType().Name}");

            if (Application.Current?.MainWindow is not MainWindow main || ReferenceEquals(window, main))
                return;

            window.Closed += (_, _) =>
            {
                try
                {
                    service.SetDialogHost(main.DialogHost);
                }
                catch (System.Exception ex)
                {
                    AppLog.Error(nameof(UiDialogHost), $"Restore dialog host failed: {ex.Message}");
                }
            };
        }
    }
}
