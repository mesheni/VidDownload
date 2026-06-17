namespace VidDownload.WPF.Services
{
    public class HandyControlMessageService : IMessageService
    {
        public void Info(string message, string title)
            => HandyControl.Controls.MessageBox.Info(message, title);

        public void Warning(string message, string title)
            => HandyControl.Controls.MessageBox.Warning(message, title);

        public void Error(string message, string title)
            => HandyControl.Controls.MessageBox.Error(message, title);
    }
}
