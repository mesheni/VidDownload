using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;
using System.Windows.Media;
using VidDownload.WPF.Control;
using VidDownload.WPF.Help;
using HandyControl.Themes;
using Xabe.FFmpeg;
using static System.Reflection.AssemblyKeyFileAttribute;
using HandyControl.Controls;
using HandyControl.Data;

namespace VidDownload.WPF
{
    public partial class MainWindow : System.Windows.Window
    {
        // Переменные для сборки команды
        private string res = null;
        private static List<string> codecList = new List<string>();
        private string codec = null;
        private string acodec = null;
        private string format = null;

        public MainWindow()
        {
            InitializeComponent();
            InitApp();
        }

        /// <summary>
        /// Кнопка загрузки видео
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void  ButDownload_Click(object sender, RoutedEventArgs e)
        {

            // Проверка на пустое поле ссылки
            if (TextBoxURL.Text.Length == 0)
            {
                await Task.Run(() => Dispatcher.Invoke(() => TextBoxAnimation())).ConfigureAwait(true);
                HandyControl.Controls.MessageBox.Error("Пустое поле ссылки!", "Ошибка!");
            }
            else
            {
                if (ComboRes.Text.Length != 0)
                    res = ComboRes.Text;
                if (ComboAudio.Text.Length != 0)
                    acodec = ComboAudio.Text;
                if (ComboFormat.Text.Length != 0)
                    format = ComboFormat.Text;
                // Проверка на пустое поле кодека
                if (ComboCodec.Text.Length == 0 || !(codecList.Exists((i) => i == ComboCodec.Text.ToString())))
                {
                    await Task.Run(() => Download(PrograssBarMain)).ConfigureAwait(true); // Загрузка видео
                }
                else
                {
                    codec = ComboCodec.Text;
                    await Task.Run(() => Download(PrograssBarMain)).ConfigureAwait(true); // Загрузка видео
                }
            }
        }

        /// <summary>
        /// Функция загрузки видео
        /// </summary>
        /// <param name="PrograssBarMain">Шкала прогресса</param>
        public async void Download(ProgressBar PrograssBarMain)
        {
            // Блокировка кнопки загрузки
            Dispatcher.Invoke(() => ButDownload.IsEnabled = false);
            
            // Создание лога
            string dateTime = DateTime.Now.ToString("yyyy-MM-dd HH_mm_ss");
            string log = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory + @"log\" + dateTime + "_log.txt");
            
            FileStream fs = new FileStream(log, FileMode.CreateNew);
            StreamWriter w = new StreamWriter(fs, Encoding.Default);

            // Запуск yt-dlp и передача команды
            await Task.Run(() =>
            {
                Process proc = new Process();

                proc.StartInfo.FileName = @".\yt-dlp.exe";
                proc.StartInfo.UseShellExecute = false;
                proc.StartInfo.RedirectStandardOutput = true;
                proc.StartInfo.CreateNoWindow = true;
                
                // Сборка команды и отправка в yt-dlp
                if (Dispatcher.Invoke(() => CheckAudio.IsChecked == true))
                {
                    proc.StartInfo.Arguments = Command.LoadAudio(acodec, TextBoxURL.Text, CheckBoxPlaylist.IsChecked);
                }
                else
                {
                    Dispatcher.Invoke(() =>
                    {
                        proc.StartInfo.Arguments = Command.LoadVideo(TextBoxURL.Text, codec, res, format, CheckBoxPlaylist.IsChecked, CheckCoder.IsChecked);
                    });
                }

                // Логирование и запись логов в файл
                proc.OutputDataReceived += new DataReceivedEventHandler((sender, e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data))
                    {
                        Dispatcher.Invoke(() =>
                        {
                            labelInfo.Content = e.Data; // Вывод логов в label
                            w.WriteLine(e.Data); // Запись логов в файл
                            PrograssBarMain.Value = ParseLog.Parse(e.Data); // Парсинг % загрузки
                        });
                    }
                });

                proc.Start();
                proc.BeginOutputReadLine();
                proc.WaitForExit();
                proc.Close();

                // Закрытие потока записи лога и разблокировка кнопки загрузки
                w.Close();
                fs.Close();

                Dispatcher.Invoke(() =>
                {
                    PrograssBarMain.Value = 0;
                    ComboCodec.Text = "";
                    ComboRes.Text = "";
                    ComboAudio.Text = "";
                    ComboFormat.Text = "";
                    TextBoxURL.Text = "";
                    
                    ButDownload.IsEnabled = true;
                    labelInfo.Content = "";
                });
            }).ConfigureAwait(true);
        }

        // Кнопка открытия папки с видео
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

        // Функция инициализации папок в приложении
        private void InitApp()
        {

            string videoPath = @".\MyVideos\";
            string logPath = @".\log\";

            string[] formats = new string[] {"", "avi", "mkv", "mp4", "webm"};

            foreach(var i in formats)
            {
                ComboFormat.Items.Add(i.ToString());
            }

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

        private void CheckAudio_Checked(object sender, RoutedEventArgs e)
        {
            LabelRes.Visibility = Visibility.Collapsed;
            ComboRes.Visibility = Visibility.Collapsed;
            LabelCodec.Visibility = Visibility.Collapsed;
            ComboCodec.Visibility = Visibility.Collapsed;
            LabelFormat.Visibility = Visibility.Collapsed;
            ComboFormat.Visibility = Visibility.Collapsed;
            CheckCoder.Visibility = Visibility.Collapsed;
            LabelCheckAudio.Visibility = Visibility.Visible;
            ComboAudio.Visibility = Visibility.Visible;
        }

        private void CheckAudio_Unchecked(object sender, RoutedEventArgs e)
        {
            LabelCheckAudio.Visibility = Visibility.Collapsed;
            ComboAudio.Visibility = Visibility.Collapsed;
            LabelCodec.Visibility = Visibility.Visible;
            ComboCodec.Visibility = Visibility.Visible;
            LabelRes.Visibility = Visibility.Visible;
            ComboRes.Visibility = Visibility.Visible;
            LabelFormat.Visibility = Visibility.Visible;
            ComboFormat.Visibility = Visibility.Visible;
            CheckCoder.Visibility = Visibility.Visible;
        }

        private void CheckBoxPlaylist_Checked(object sender, RoutedEventArgs e)
        {
            LabelLink.Content = "Поле для ссылки на плейлист:";
        }

        private void CheckBoxPlaylist_Unchecked(object sender, RoutedEventArgs e)
        {
            LabelLink.Content = "Поле для ссылки на видео:";
        }

        private void CheckCoder_Checked(object sender, RoutedEventArgs e)
        {
            ComboFormat.Items.Add("mov");
            HandyControl.Controls.MessageBox.Info("Перекодирование видео может потребовать длительного времени. Лучше воспользуйтесь сторонними конвертерами.");
        }

        private void CheckCoder_Unchecked(object sender, RoutedEventArgs e)
        {
            ComboFormat.Items.Remove("mov");
        }

        private void ButtonHelp_Click(object sender, RoutedEventArgs e)
        {
            HelpWindow help = new HelpWindow();
            help.ShowDialog();
        }

        private void TextBoxAnimation()
        {
            ColorAnimation colorAnimation = new ColorAnimation
            {
                From = Colors.White, // Исходный цвет фона
                To = (Color)ColorConverter.ConvertFromString("#ff4f4f"), // Целевой цвет фона
                AutoReverse = true, // Автоматически вернуться к исходному цвету
                Duration = TimeSpan.FromSeconds(0.5f), // Длительность анимации (1 секунда)
                RepeatBehavior = new RepeatBehavior(2) // Повторять анимацию 2 раза
            };

            TextBoxURL.Background = new SolidColorBrush(Colors.White); // Установка исходного цвета фона

            // Создание и применение анимации
            TextBoxURL.Background.BeginAnimation(SolidColorBrush.ColorProperty, colorAnimation);
        }

    }
}
