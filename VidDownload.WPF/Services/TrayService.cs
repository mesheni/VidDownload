using System;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using H.NotifyIcon;
using H.NotifyIcon.Core;
using VidDownload.WPF.Resources;

namespace VidDownload.WPF.Services
{
    /// <summary>
    /// Иконка приложения в системном трее (H.NotifyIcon): открытие окна по двойному
    /// клику, контекстное меню, balloon-уведомления когда окно скрыто.
    /// </summary>
    public class TrayService : ITrayService
    {
        private TaskbarIcon? _icon;

        public event EventHandler? ShowRequested;
        public event EventHandler? OpenDownloadsRequested;
        public event EventHandler? ExitRequested;

        /// <summary>Иконка реально создана в трее. Если false, сворачивание в трей недоступно.</summary>
        public bool IsAvailable => _icon is { IsCreated: true };

        public void Initialize()
        {
            if (_icon != null)
                return;

            try
            {
                _icon = new TaskbarIcon
                {
                    ToolTipText = "VidDownload",
                    Icon = LoadIcon()
                };
            }
            catch (Exception ex)
            {
                // Трей — необязательная функция: приложение должно работать и без него
                AppLog.Error(nameof(TrayService), $"Tray icon init failed: {ex}");
                _icon = null;
                return;
            }

            _icon.TrayLeftMouseDoubleClick += (_, _) => ShowRequested?.Invoke(this, EventArgs.Empty);
            _icon.ContextMenu = BuildMenu();

            // TaskbarIcon создаёт иконку в трее только в обработчике Loaded (т.е. при
            // размещении в визуальном дереве из XAML). При создании из кода Loaded
            // не срабатывает — без ForceCreate иконка не появляется никогда.
            try
            {
                _icon.ForceCreate(enablesEfficiencyMode: false);
            }
            catch (Exception ex)
            {
                AppLog.Error(nameof(TrayService), $"Tray icon create failed: {ex}");
                Dispose();
                return;
            }

            // Пересобираем меню при смене языка
            LocalizedStrings.Instance.PropertyChanged += OnLanguageChanged;
        }

        /// <summary>
        /// Готовая System.Drawing.Icon из иконки самого exe (ApplicationIcon).
        /// Конвертация ImageSource из PNG в иконку через H.NotifyIcon ненадёжна
        /// (ArgumentException "picture must be a picture that can be used as a Icon"),
        /// поэтому используем извлечение уже готовой иконки из исполняемого файла.
        /// </summary>
        private static Icon LoadIcon()
        {
            try
            {
                string? exePath = Environment.ProcessPath;
                if (!string.IsNullOrEmpty(exePath) && File.Exists(exePath))
                {
                    Icon? extracted = Icon.ExtractAssociatedIcon(exePath);
                    if (extracted != null)
                        return extracted;
                }
            }
            catch (Exception ex)
            {
                AppLog.Error(nameof(TrayService), $"Icon extraction failed: {ex.Message}");
            }

            return SystemIcons.Application;
        }

        private void OnLanguageChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == null || string.IsNullOrEmpty(e.PropertyName))
                RebuildMenu();
        }

        private void RebuildMenu()
        {
            if (_icon != null)
                _icon.ContextMenu = BuildMenu();
        }

        private ContextMenu BuildMenu()
        {
            var loc = LocalizedStrings.Instance;
            var menu = new ContextMenu();

            var showItem = new MenuItem { Header = loc["TrayShow"], FontWeight = FontWeights.Bold };
            showItem.Click += (_, _) => ShowRequested?.Invoke(this, EventArgs.Empty);

            var openItem = new MenuItem { Header = loc["TrayOpenFolder"] };
            openItem.Click += (_, _) => OpenDownloadsRequested?.Invoke(this, EventArgs.Empty);

            var exitItem = new MenuItem { Header = loc["TrayExit"] };
            exitItem.Click += (_, _) => ExitRequested?.Invoke(this, EventArgs.Empty);

            menu.Items.Add(showItem);
            menu.Items.Add(openItem);
            menu.Items.Add(new Separator());
            menu.Items.Add(exitItem);
            return menu;
        }

        public void ShowBalloon(string title, string message)
        {
            try
            {
                _icon?.ShowNotification(title, message, NotificationIcon.Info);
            }
            catch (Exception ex)
            {
                AppLog.Error(nameof(TrayService), $"ShowNotification failed: {ex.Message}");
            }
        }

        public void Dispose()
        {
            if (_icon == null)
                return;

            LocalizedStrings.Instance.PropertyChanged -= OnLanguageChanged;
            _icon.Dispose();
            _icon = null;
        }
    }
}
