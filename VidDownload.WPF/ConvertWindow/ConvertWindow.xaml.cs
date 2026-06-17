using Microsoft.Extensions.DependencyInjection;
using System.Threading.Tasks;
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
            Loaded += ConvertWindow_Loaded;
        }

        private async void ConvertWindow_Loaded(object sender, System.Windows.RoutedEventArgs e)
        {
            if (DataContext is ConvertViewModel vm)
            {
                await vm.InitializeAsync();
            }
        }
    }
}
