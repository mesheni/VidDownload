using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace VidDownload.WPF.ConvertWindow
{
    /// <summary>
    /// Логика взаимодействия для ConvertWindow.xaml
    /// </summary>
    public partial class ConvertWindow : Window
    {
        public ConvertWindow()
        {
            InitializeComponent();
        }

        /*
        private void Convert_Click(object sender, RoutedEventArgs e)
        {
            
            if (string.IsNullOrEmpty(Input.Text) || string.IsNullOrEmpty(Output.Text))
            {
                MessageBox.Show("Введите путь к файлу и выберите папку для сохранения");
                return;
            }
        }
        */
    }
}
