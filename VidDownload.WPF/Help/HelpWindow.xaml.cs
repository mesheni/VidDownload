using System.Diagnostics;
using System.Reflection;
using System.Windows;
using System.Windows.Navigation;
using VidDownload.WPF.Resources;
using VidDownload.WPF.Services;

namespace VidDownload.WPF.Help
{
    public partial class HelpWindow
    {
        public HelpWindow()
        {
            InitializeComponent();
            // Динамическая локализация: окно обновляется при смене языка,
            // в отличие от статических привязок к ресурсам
            DataContext = this;
            UiDialogHost.Attach(this);
        }

        public LocalizedStrings LocalizedStrings => LocalizedStrings.Instance;

        /// <summary>Версия приложения для страницы «О программе».</summary>
        public string AppVersion =>
            Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "?";

        private void OpenUri(string url)
        {
            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
        }

        private void Hyperlink_Vk(object sender, RequestNavigateEventArgs e)
        {
            OpenUri(e.Uri.AbsoluteUri);
            e.Handled = true;
        }

        private void Hyperlink_Gh(object sender, RequestNavigateEventArgs e)
        {
            OpenUri(e.Uri.AbsoluteUri);
            e.Handled = true;
        }

        private void Hyperlink_Vk_Click(object sender, RoutedEventArgs e) => OpenUri("https://t.me/mesheni_channel");

        private void Hyperlink_Gh_Click(object sender, RoutedEventArgs e) => OpenUri("https://github.com/mesheni");

        private void imgJojack_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            OpenUri("https://vk.com/jojacki");
        }
    }
}
