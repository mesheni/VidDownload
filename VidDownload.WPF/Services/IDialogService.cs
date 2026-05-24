using System.Threading.Tasks;

namespace VidDownload.WPF.Services
{
    public interface IDialogService
    {
        Task<bool> AskAsync(string message, string title);
    }
}
