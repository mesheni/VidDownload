using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using VidDownload.WPF.Control;
using VidDownload.WPF.Help;
using Octokit.Clients;
using Octokit;
using System.Windows.Shapes;
using System.Net;
using System.Globalization;

/*

     _____  
   .'     `.
  /         \
 |           |      Программа для скачивания видео с YouTube и других платформ.
 '.  +^^^+  .'      Current version: 0.7.0
   `. \./ .'        by mesh
     |_|_|          2024г.
     (___)          First commit: 20 августа, 2022г.
     (___)
     `---'

Russian:
Не стоит относиться к коду этой программы слишком серьезно. Я создаю ее для изучения и совершенствования, а также 
в рамках своих исследований. Хотя я, возможно, и не профессиональный программист, вы можете просмотреть код и 
внести любые изменения или улучшения, которые сочтете необходимыми.

English:
Do not take this program's code too seriously. I am creating it for my own learning and improvement, 
and as part of my studies. While I may not be a professional programmer, you are welcome to review 
the code and suggest any fixes or improvements you see fit.

Мои контакты:
Email - bunin.ivan14@yandex.ru
GitHub - https://github.com/mesheni
Telegram - https://t.me/meshenii
VK - https://vk.com/mesheni
Twitter/X - https://x.com/meshenii

*/

namespace VidDownload.WPF
{
    public partial class MainWindow : System.Windows.Window
    {
        // Переменные для сборки команды
        private static List<string> codecList = new();
        Settings settings = new();
        public MainWindow()
        {
            InitializeComponent();
            InitApp();
            CheckUpdateAsync();
        }


        /// <summary>
        /// Обрабатывает событие click для кнопки загрузки. Инициирует асинхронный процесс загрузки видео.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void ButDownload_Click(object sender, RoutedEventArgs e)
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
                    settings.Resolution = ComboRes.Text;
                if (ComboAudio.Text.Length != 0)
                    settings.AudioCodec = ComboAudio.Text;
                if (ComboFormat.Text.Length != 0)
                    settings.Format = ComboFormat.Text;
                // Проверка на пустое поле кодека
                if (ComboCodec.Text.Length == 0 || !(codecList.Exists((i) => i == ComboCodec.Text.ToString())))
                {
                    await Task.Run(() => Download(ProgressBarMain)).ConfigureAwait(true); // Загрузка видео
                }
                else
                {
                    settings.VideoCodec = ComboCodec.Text;
                    await Task.Run(() => Download(ProgressBarMain)).ConfigureAwait(true); // Загрузка видео
                }
            }
        }

        /// <summary>
        /// Асинхронная функция загрузки видео. 
        /// </summary>
        /// <param name="PrograssBarMain">Шкала прогресса</param>
        public async void Download(ProgressBar PrograssBarMain)
        {
            // Блокировка кнопки загрузки
            Dispatcher.Invoke(() => ButDownload.IsEnabled = false);

            // Создание лога
            string dateTime = DateTime.Now.ToString("yyyy-MM-dd HH_mm_ss");
            string log = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory + @"log\" + dateTime + "_log.txt");

            FileStream fs = new(log, System.IO.FileMode.CreateNew);

            // Запуск yt-dlp и передача команды
            await Task.Run(() =>
            {
                Process proc = new();

                proc.StartInfo.FileName = @".\yt-dlp.exe";
                proc.StartInfo.UseShellExecute = false;
                proc.StartInfo.RedirectStandardOutput = true;
                proc.StartInfo.CreateNoWindow = true;

                // Сборка команды и отправка в yt-dlp
                if (Dispatcher.Invoke(() => CheckAudio.IsChecked == true))
                {
                    proc.StartInfo.Arguments = Command.LoadAudio(settings, TextBoxURL.Text, CheckBoxPlaylist.IsChecked);
                }
                else
                {
                    Dispatcher.Invoke(() =>
                    {
                        proc.StartInfo.Arguments = Command.LoadVideo(TextBoxURL.Text, settings, CheckBoxPlaylist.IsChecked, CheckCoder.IsChecked);
                    });
                }

                StreamWriter w = new(fs, Encoding.Default);
                // Логирование и запись логов в файл
                proc.OutputDataReceived += new DataReceivedEventHandler((sender, e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data))
                    {
                        Dispatcher.Invoke(() =>
                        {
                            labelInfo.Content = e.Data; // Вывод логов в label
                            w.WriteLine(e.Data); // Запись логов в файл
                            ProgressBarMain.Value = ParseLog.Parse(e.Data); // Парсинг % загрузки
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
                    ProgressBarMain.Value = 0;
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

            string[] formats = new string[] { "", "avi", "mkv", "mp4", "webm" };

            foreach (var i in formats)
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

        private async void CheckUpdateAsync()
        {
            if (CheckForInternetConnection().Result)
                await Task.Run(() =>
                {
                    bool fileNotFound = false;
                    string? links = "";
                    string currentVer = string.Empty;
                    MessageBoxResult res = new();

                    var client = new GitHubClient(new Octokit.ProductHeaderValue("VidDownload"));

                    var releases = client.Repository.Release.GetLatest("yt-dlp", "yt-dlp");
                    var latest = releases;

                    string latestVer = latest.Result.TagName.Replace(".", "");

                    try
                    {
                        var versionInfo = FileVersionInfo.GetVersionInfo("yt-dlp.exe");
                        currentVer = versionInfo.FileVersion.Replace(".", "");

                        if (Convert.ToInt32(currentVer) < Convert.ToInt32(latestVer))
                        {
                            res = HandyControl.Controls.MessageBox.Ask($"Текущая версия: {currentVer} \nПоследняя версия: {latestVer}\nПодтвердите начало обновления.", "Доступна новая версия yt-dlp!");
                        }
                    }
                    catch (Exception ex)
                    {
                        HandyControl.Controls.MessageBox.Error("При проверке обновлений не был найден файл yt-dlp!\nБудет загружена последняя версия.", "Ошибка!");
                        fileNotFound = true;
                    }

                    if (res == MessageBoxResult.OK || fileNotFound)
                    {
                        foreach (var release in releases.Result.Assets)
                        {
                            if (release.BrowserDownloadUrl.Contains("yt-dlp.exe"))
                            {
                                links = release.BrowserDownloadUrl;
                            }

                        }

                        Dispatcher.Invoke(() =>
                        {
                            ButDownload.IsEnabled = false;
                            labelInfo.Content = "Идет загрузка обновления yt-dlp!";
                        });

                        var wc = new WebClient();

                        wc.DownloadProgressChanged += (sender, args) =>
                        {
                            Dispatcher.Invoke(() =>
                            {
                                ProgressBarMain.Value = args.ProgressPercentage;
                            });
                        };

                        wc.Headers.Add(HttpRequestHeader.UserAgent, "MyUserAgent");
                        wc.DownloadFileAsync(new Uri(links), "yt-dlp.exe");

                        wc.DownloadFileCompleted += (sender, args) =>
                        {
                            Dispatcher.Invoke(() =>
                            {
                                ProgressBarMain.Value = 0;
                                labelInfo.Content = "";
                                ButDownload.IsEnabled = true;
                            });
                            HandyControl.Controls.MessageBox.Info($"Версия yt-dlp обновлена до {latest.Result.TagName}", "Обновление завершено!");
                        
                        };

                    }
                }).ConfigureAwait(false);
        }

        private void CheckAudio_Checked(object sender, RoutedEventArgs e)
        {
            LabelRes.Visibility = Visibility.Collapsed;
            ComboRes.Visibility = Visibility.Collapsed;
            LabelCodec.Visibility = Visibility.Collapsed;
            ComboCodec.Visibility = Visibility.Collapsed;
            LabelFormat.Visibility = Visibility.Collapsed;
            ComboFormat.Visibility = Visibility.Collapsed;
            LabelCheckAudio.Visibility = Visibility.Visible;
            ComboAudio.Visibility = Visibility.Visible;
            CheckCoder.IsEnabled = false;
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
            CheckCoder.IsEnabled = true;
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
            HelpWindow help = new();
            help.ShowDialog();
        }

        private void ButtonConvert_Click(object sender, RoutedEventArgs e)
        {
            ConvertWindow.ConvertWindow convert = new();
            convert.ShowDialog();
        }

        private void TextBoxAnimation()
        {
            ColorAnimation colorAnimation = new()
            {
                From = Colors.White, // Исходный цвет фона
                To = (Color)ColorConverter.ConvertFromString("#ff4f4f"), // Целевой цвет фона
                AutoReverse = true, // Автоматически вернуться к исходному цвету
                Duration = TimeSpan.FromSeconds(0.5f), // Длительность анимации (0,5 секунд)
                RepeatBehavior = new RepeatBehavior(2) // Повторять анимацию 2 раза
            };

            TextBoxURL.Background = new SolidColorBrush(Colors.White); // Установка исходного цвета фона

            // Создание и применение анимации
            TextBoxURL.Background.BeginAnimation(SolidColorBrush.ColorProperty, colorAnimation);
        }

        public async Task<bool> CheckForInternetConnection(int timeoutMs = 1000, string url = null)
        {
            bool result = false;
            await Task.Run(() => 
            {
                try
                {
                    url ??= CultureInfo.InstalledUICulture switch
                    {
                        //{ Name: var n } when n.StartsWith("fa") => // Iran
                        //    "http://www.aparat.com",
                        { Name: var n } when n.StartsWith("ru") => // Russian
                            "https://ya.ru/",
                        _ =>
                            "http://www.gstatic.com/generate_204",
                    };

                    var request = (HttpWebRequest)WebRequest.Create(url);
                    request.KeepAlive = false;
                    request.Timeout = timeoutMs;
                    using (var response = (HttpWebResponse)request.GetResponse())
                        result = true;
                }
                catch
                {
                    result = false;
                }
            }).ConfigureAwait(false);

            return result;
        }

    }
}
