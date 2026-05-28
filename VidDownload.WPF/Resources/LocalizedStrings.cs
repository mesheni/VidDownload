namespace VidDownload.WPF.Resources
{
    using System;
    using System.ComponentModel;
    using System.Globalization;
    using System.Runtime.CompilerServices;
    using System.Threading;
    using VidDownload.WPF.Services;

    public class LocalizedStrings : ILocalizationService, INotifyPropertyChanged
    {
        private static readonly Lazy<LocalizedStrings> _instance = new(() => new LocalizedStrings());
        public static LocalizedStrings Instance => _instance.Value;

        private string _currentLanguage = "RU";

        public CultureInfo CurrentCulture => Res.Culture ?? CultureInfo.GetCultureInfo("ru");

        public string CurrentLanguage
        {
            get => _currentLanguage;
            private set
            {
                if (_currentLanguage != value)
                {
                    _currentLanguage = value;
                    OnPropertyChanged();
                }
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        public string this[string key] => Res.ResourceManager.GetString(key, Res.Culture)
            ?? Res.ResourceManager.GetString(key, CultureInfo.InvariantCulture)
            ?? key;

        public string GetString(string key) => this[key];

        public void SetLanguage(string cultureCode)
        {
            var culture = new CultureInfo(cultureCode);
            Res.Culture = culture;
            Thread.CurrentThread.CurrentUICulture = culture;
            Thread.CurrentThread.CurrentCulture = culture;
            CurrentLanguage = cultureCode.ToUpper();
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(null));
        }

        private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
