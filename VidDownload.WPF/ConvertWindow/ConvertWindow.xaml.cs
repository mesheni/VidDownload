using Microsoft.Extensions.DependencyInjection;
using VidDownload.WPF.Services;
using VidDownload.WPF.ViewModels;

namespace VidDownload.WPF.ConvertWindow
{
    public partial class ConvertWindow : System.Windows.Window
    {
        public ConvertWindow()
        {
            InitializeComponent();
            DataContext = AppServices.ServiceProvider.GetRequiredService<ConvertViewModel>();
        }
    }
}
