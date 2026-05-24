using System.Threading.Tasks;
using System.Windows;

namespace VidDownload.WPF.Services
{
    public class HandyControlDialogService : IDialogService
    {
        public Task<bool> AskAsync(string message, string title)
        {
            var result = HandyControl.Controls.MessageBox.Ask(message, title);
            return Task.FromResult(result == MessageBoxResult.OK || result == MessageBoxResult.Yes);
        }
    }
}
