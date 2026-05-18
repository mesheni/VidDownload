using VidDownload.WPF.ViewModels;

namespace VidDownload.WPF
{
    public partial class MainWindow : System.Windows.Window
    {
        public MainWindow()
        {
            InitializeComponent();
            var vm = new MainViewModel();
            DataContext = vm;
            _ = vm.CheckUpdateAsync();
        }
    }
}
