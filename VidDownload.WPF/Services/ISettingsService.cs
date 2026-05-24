using System.Threading.Tasks;

namespace VidDownload.WPF.Services
{
    public interface ISettingsService
    {
        Task<UserSettings> LoadAsync();
        Task SaveAsync(UserSettings settings);
    }
}
