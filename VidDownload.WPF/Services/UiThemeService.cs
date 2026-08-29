using System;
using System.Windows;
using System.Windows.Media;
using Wpf.Ui.Appearance;
using Wpf.Ui.Controls;

namespace VidDownload.WPF.Services
{
    public enum AppThemePreference
    {
        /// <summary>Следовать за системной темой Windows.</summary>
        Auto,
        Light,
        Dark
    }

    /// <summary>
    /// Управление темой приложения (Авто/Светлая/Тёмная) поверх ApplicationThemeManager:
    /// применяется фирменный красный акцент и подменяется собственная палитра
    /// поверхностей Dark/Light.Colors.xaml. Потребители палитры используют
    /// DynamicResource, поэтому смена темы применяется к открытым окнам сразу.
    /// </summary>
    public static class UiThemeService
    {
        private static readonly Uri DarkPaletteUri = new("/Themes/Dark.Colors.xaml", UriKind.Relative);
        private static readonly Uri LightPaletteUri = new("/Themes/Light.Colors.xaml", UriKind.Relative);

        /// <summary>Фирменный красный акцент — синхронизирован с Themes/Shared.xaml.</summary>
        private static readonly Color AccentColor = Color.FromRgb(0xE5, 0x48, 0x4D);

        private const WindowBackdropType Backdrop = WindowBackdropType.Mica;

        /// <summary>Текущее выбранное пользователем предпочтение.</summary>
        public static AppThemePreference Preference { get; private set; } = AppThemePreference.Dark;

        /// <summary>Фактически применённая тема (после разрешения Auto).</summary>
        public static ApplicationTheme CurrentTheme { get; private set; } = ApplicationTheme.Unknown;

        /// <summary>Уведомляет UI о смене предпочтения (например, для иконки-переключателя).</summary>
        public static event Action? PreferenceChanged;

        /// <summary>Применяет сохранённое предпочтение пользователя при старте.</summary>
        public static void Initialize(AppThemePreference preference) => SetPreference(preference);

        public static void SetPreference(AppThemePreference preference)
        {
            Preference = preference;

            var theme = preference switch
            {
                AppThemePreference.Light => ApplicationTheme.Light,
                AppThemePreference.Dark => ApplicationTheme.Dark,
                _ => ResolveSystemTheme()
            };

            try
            {
                ApplicationThemeManager.Apply(theme, Backdrop);
            }
            catch (Exception ex)
            {
                // Mica может быть недоступен (старые ОС / удалённые сеансы) — тема всё равно применена частично
                AppLog.Error(nameof(UiThemeService), $"Apply({theme}) failed: {ex.Message}");
            }

            // Менеджер применяет системный акцент — возвращаем фирменный после каждой смены
            try
            {
                ApplicationAccentColorManager.Apply(AccentColor, theme, false, false);
            }
            catch (Exception ex)
            {
                AppLog.Error(nameof(UiThemeService), $"Apply accent failed: {ex.Message}");
            }

            ApplyPalette(preference == AppThemePreference.Light ? LightPaletteUri : DarkPaletteUri);

            CurrentTheme = theme;
            PreferenceChanged?.Invoke();
        }

        public static void CyclePreference() =>
            SetPreference(Preference switch
            {
                AppThemePreference.Auto => AppThemePreference.Light,
                AppThemePreference.Light => AppThemePreference.Dark,
                _ => AppThemePreference.Auto
            });

        /// <summary>Разбирает сохранённую настройку ("Auto"/"Light"/"Dark"); по умолчанию — тёмная.</summary>
        public static AppThemePreference TryParse(string? value) =>
            value?.Trim().ToLowerInvariant() switch
            {
                "auto" => AppThemePreference.Auto,
                "light" => AppThemePreference.Light,
                "dark" => AppThemePreference.Dark,
                _ => AppThemePreference.Dark
            };

        private static ApplicationTheme ResolveSystemTheme()
        {
            try
            {
                return ApplicationThemeManager.GetSystemTheme() switch
                {
                    SystemTheme.Light => ApplicationTheme.Light,
                    SystemTheme.Dark => ApplicationTheme.Dark,
                    _ => ApplicationTheme.Dark
                };
            }
            catch (Exception ex)
            {
                AppLog.Error(nameof(UiThemeService), $"GetSystemTheme failed: {ex.Message}");
            }
            return ApplicationTheme.Dark;
        }

        /// <summary>Подменяет собственную палитру поверхностей, сохраняя её последним словарём ресурсов.</summary>
        private static void ApplyPalette(Uri paletteUri)
        {
            var merged = Application.Current.Resources.MergedDictionaries;

            for (int i = merged.Count - 1; i >= 0; i--)
            {
                if (ReferenceEquals(merged[i].Source, paletteUri))
                    return; // уже применена

                if (IsPalette(merged[i].Source))
                    merged.RemoveAt(i);
            }

            merged.Add(new ResourceDictionary { Source = paletteUri });
        }

        private static bool IsPalette(System.Uri? source) =>
            source != null &&
            (source.OriginalString.EndsWith("Dark.Colors.xaml", StringComparison.OrdinalIgnoreCase) ||
             source.OriginalString.EndsWith("Light.Colors.xaml", StringComparison.OrdinalIgnoreCase));
    }
}
