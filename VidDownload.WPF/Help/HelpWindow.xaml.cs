using System.Diagnostics;
using System.Windows;
using System.Windows.Navigation;
using VidDownload.WPF.Resources;

namespace VidDownload.WPF.Help
{
    public partial class HelpWindow : Window
    {
        public HelpWindow()
        {
            InitializeComponent();
            DataContext = LocalizedStrings.Instance;
        }

        private void Hyperlink_Vk(object sender, RequestNavigateEventArgs e)
        {
            Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true });
            e.Handled = true;
        }

        private void Hyperlink_Gh(object sender, RequestNavigateEventArgs e)
        {
            Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true });
            e.Handled = true;
        }

        private void imgJojack_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            Process.Start(new ProcessStartInfo("https://vk.com/jojacki") { UseShellExecute = true });
        }
    }
}
