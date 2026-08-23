using System;
using System.Windows.Threading;

namespace VidDownload.WPF.Services
{
    public interface IClipboardMonitorService : IDisposable
    {
        /// <summary>Включает/выключает отслеживание буфера обмена.</summary>
        bool IsEnabled { get; set; }

        /// <summary>Новая http(s)-ссылка, скопированная в буфер обмена.</summary>
        event EventHandler<string>? UrlDetected;
    }

    /// <summary>
    /// Мониторинг буфера обмена: раз в секунду проверяет текст в буфере и,
    /// если это новая http(s)-ссылка, сообщает через событие (UI-поток).
    /// </summary>
    public class ClipboardMonitorService : IClipboardMonitorService
    {
        private readonly DispatcherTimer _timer = new() { Interval = TimeSpan.FromSeconds(1) };
        private string _lastText = string.Empty;

        public event EventHandler<string>? UrlDetected;

        public ClipboardMonitorService()
        {
            _timer.Tick += OnTick;
        }

        public bool IsEnabled
        {
            get => _timer.IsEnabled;
            set
            {
                if (value == _timer.IsEnabled)
                    return;

                if (value)
                {
                    // Не реагируем на то, что уже лежит в буфере в момент включения
                    _lastText = SafeGetText();
                    _timer.Start();
                }
                else
                {
                    _timer.Stop();
                }
            }
        }

        private void OnTick(object? sender, EventArgs e)
        {
            string text = SafeGetText();
            if (text == _lastText)
                return;

            _lastText = text;
            if (UrlHelper.LooksLikeVideoReference(text))
                UrlDetected?.Invoke(this, text);
        }

        private static string SafeGetText()
        {
            try
            {
                return System.Windows.Clipboard.ContainsText()
                    ? System.Windows.Clipboard.GetText().Trim()
                    : string.Empty;
            }
            catch
            {
                // Буфер может быть временно занят другим процессом — пропускаем тик
                return string.Empty;
            }
        }

        public void Dispose()
        {
            _timer.Stop();
        }
    }
}
