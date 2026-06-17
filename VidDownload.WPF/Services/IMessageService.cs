namespace VidDownload.WPF.Services
{
    public interface IMessageService
    {
        void Info(string message, string title);
        void Warning(string message, string title);
        void Error(string message, string title);
    }
}
