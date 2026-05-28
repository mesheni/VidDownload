using System.ComponentModel;
using System.Globalization;

namespace VidDownload.WPF.Services
{
    public interface ILocalizationService : INotifyPropertyChanged
    {
        CultureInfo CurrentCulture { get; }
        string CurrentLanguage { get; }
        void SetLanguage(string cultureCode);
        string GetString(string key);
        string this[string key] { get; }
    }
}
