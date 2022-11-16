using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
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
using System.Windows.Navigation;
using System.Windows.Shapes;
using VidDownload.WPF.Control;

namespace VidDownload.WPF
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private int res = 1080;
        private static List<string> codecList = new List<string>();
        private string codec = "av01";
        private double[] progRes = new double[2];
        private ParseLog parseLog = new ParseLog();

        public MainWindow()
        {
            InitializeComponent();
            InitApp();
        }

        private async void  ButDownload_Click(object sender, RoutedEventArgs e)
        {
            if (TextBoxURL.Text == "" || ComboRes.Text == "")
            {
                MessageBox.Show("Пустое поле ссылки или поле разрешения!", "Ошибка!",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            else
            {
                if (int.TryParse(ComboRes.Text, out res))
                {
                    if (ComboCodec.Text == "" || !(codecList.Exists((i) => i == ComboCodec.Text.ToString())))
                    {
                        await Task.Run(() => Download(PrograssBarMain));
                    }
                    else
                    {
                        codec = ComboCodec.Text;
                        await Task.Run(() => Download(PrograssBarMain));
                    }

                }
                else
                {
                    MessageBox.Show("Некорректное значение в поле \"Расширение\"", "Ошибка!",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        public async void Download(ProgressBar PrograssBarMain)
        {
            Dispatcher.Invoke(() => ButDownload.IsEnabled = false);

            string dateTime = DateTime.Now.ToString("yyyy-MM-dd HH_mm_ss");
            string log = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory + @"log\" + dateTime + "_log.txt");

            FileStream fs = new FileStream(log, FileMode.CreateNew);
            StreamWriter w = new StreamWriter(fs, Encoding.Default);

            await Task<string>.Run(() =>
            {
                System.Diagnostics.Process proc = new System.Diagnostics.Process();

                proc.StartInfo.FileName = @".\yt-dlp.exe";
                proc.StartInfo.UseShellExecute = false;
                proc.StartInfo.RedirectStandardOutput = true;
                proc.StartInfo.CreateNoWindow = true;
                
                if (Dispatcher.Invoke(() => CheckBoxPlaylist.IsChecked == true))
                {
                    proc.StartInfo.Arguments = $"yt-dlp -S \"+codec:{codec},res:{res},fps\" -o \"./MyVideos/%(playlist)s/%(playlist_index)s- %(title)s.%(ext)s\" \"{Dispatcher.Invoke(() => TextBoxURL.Text)}\"";
                }
                else
                {
                    proc.StartInfo.Arguments = $"yt-dlp -S \"+codec:{codec},res:{res},fps\" -P \"./MyVideos\" {Dispatcher.Invoke(() => TextBoxURL.Text)}";
                }

                proc.OutputDataReceived += new DataReceivedEventHandler((sender, e) =>
                {
                    if (!String.IsNullOrEmpty(e.Data))
                    {
                        Dispatcher.Invoke(() =>
                        {
                            labelInfo.Content = e.Data;
                            w.WriteLine(e.Data);
                            PrograssBarMain.Value = parseLog.Parse(e.Data);

                        });
                    }
                });

                proc.Start();
                proc.BeginOutputReadLine();
                proc.WaitForExit();
                proc.Close();

                w.Close();
                fs.Close();

                Dispatcher.Invoke(() => ButDownload.IsEnabled = true);
                Dispatcher.Invoke(() => labelInfo.Content = "");
            });
        }

        private void ButOpenFolder_Click(object sender, RoutedEventArgs e)
        {
            string path = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory + @"MyVideos\");

            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }

            string argument = "/open, \"" + path;
            System.Diagnostics.Process.Start("explorer.exe", argument);
        }

        private void InitApp()
        {
            string videoPath = @".\MyVideos\";
            string logPath = @".\log\";

            if (!Directory.Exists(videoPath))
            {
                Directory.CreateDirectory(videoPath);
            }
            if (!Directory.Exists(logPath))
            {
                Directory.CreateDirectory(logPath);
            }

            foreach (var i in ComboCodec.Items)
            {
                codecList.Add(i.ToString());
            }
        }
    }
}
