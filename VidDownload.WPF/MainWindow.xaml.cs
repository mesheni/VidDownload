using Microsoft.Extensions.DependencyInjection;
using VidDownload.WPF.Services;
using VidDownload.WPF.ViewModels;

namespace VidDownload.WPF
{
    public partial class MainWindow : System.Windows.Window
    {
        public MainWindow()
        {
            InitializeComponent();
            DataContext = AppServices.ServiceProvider.GetRequiredService<MainViewModel>();
        }
    }
}
