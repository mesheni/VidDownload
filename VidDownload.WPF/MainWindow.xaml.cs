using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using VidDownload.WPF.Control;
using VidDownload.WPF.Help;
using VidDownload.WPF.QueueWindow;
using HandyControl.Data;

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
        private List<string> codecList = new();
        Settings settings = new();
        private UpdateService? _updateService;
        private DownloadService? _downloadService;
        private CancellationTokenSource? _cts;

        public MainWindow()
        {
            InitializeComponent();
            InitApp();
            LoadSavedSettings();
            _updateService = new UpdateService();
            _updateService.ProgressChanged += (progress) =>
                Dispatcher.Invoke(() => ProgressBarMain.Value = progress);
            _updateService.StatusChanged += (status) =>
                Dispatcher.Invoke(() => labelInfo.Content = status);
            _updateService.UpdateCompleted += (success, message) =>
            {
                Dispatcher.Invoke(() =>
                {
                    ProgressBarMain.Value = 0;
                    labelInfo.Content = "";
                    ButDownload.IsEnabled = true;
                });
                if (success)
                    HandyControl.Controls.MessageBox.Info(message, "Обновление завершено!");
                else
                    HandyControl.Controls.MessageBox.Error(message, "Ошибка обновления!");
            };
            CheckUpdateAsync();
        }

        /// <summary>
        /// Загружает сохранённые настройки и применяет их к UI.
        /// </summary>
        private void LoadSavedSettings()
        {
            settings = Settings.Load();
            ComboRes.Text = settings.Resolution;
            ComboCodec.Text = settings.VideoCodec;
            ComboAudio.Text = settings.AudioCodec;
            ComboFormat.Text = settings.Format;

            if (settings.Theme == "Light")
                ApplyTheme(false);

            try
            {
                if (settings.Language == "en")
                {
                    Thread.CurrentThread.CurrentUICulture = new CultureInfo("en");
                    Thread.CurrentThread.CurrentCulture = new CultureInfo("en");
                }
            }
            catch { }
        }

        /// <summary>
        /// Сохраняет текущие настройки из UI.
        /// </summary>
        private void SaveCurrentSettings()
        {
            if (ComboRes.Text.Length != 0)
                settings = settings with { Resolution = ComboRes.Text };
            if (ComboAudio.Text.Length != 0)
                settings = settings with { AudioCodec = ComboAudio.Text };
            if (ComboFormat.Text.Length != 0)
                settings = settings with { Format = ComboFormat.Text };
            bool hasValidCodec = ComboCodec.Text.Length != 0 && codecList.Exists(i => i == ComboCodec.Text);
            if (hasValidCodec)
                settings = settings with { VideoCodec = ComboCodec.Text };
            settings.Save();
        }

        /// <summary>
        /// Обрабатывает событие click для кнопки загрузки. Инициирует асинхронный процесс загрузки видео.
        /// </summary>
        private async void ButDownload_Click(object sender, RoutedEventArgs e)
        {
            if (TextBoxURL.Text.Length == 0)
            {
                await Task.Run(() => Dispatcher.Invoke(() => TextBoxAnimation())).ConfigureAwait(true);
                HandyControl.Controls.MessageBox.Error("Пустое поле ссылки!", "Ошибка!");
                return;
            }

            SaveCurrentSettings();

            ButDownload.IsEnabled = false;
            ButStop.IsEnabled = true;

            _cts = new CancellationTokenSource();
            var token = _cts.Token;

            _downloadService = new DownloadService();
            _downloadService.OutputReceived += (output) =>
                Dispatcher.Invoke(() => labelInfo.Content = output);
            _downloadService.ProgressChanged += (progress) =>
                Dispatcher.Invoke(() => ProgressBarMain.Value = progress);
            _downloadService.DownloadCompleted += (success, message) =>
            {
                Dispatcher.Invoke(() =>
                {
                    ProgressBarMain.Value = 0;
                    ButDownload.IsEnabled = true;
                    ButStop.IsEnabled = false;
                    _cts = null;
                    if (success)
                    {
                        ComboCodec.Text = settings.VideoCodec;
                        ComboRes.Text = settings.Resolution;
                        ComboAudio.Text = settings.AudioCodec;
                        ComboFormat.Text = settings.Format;
                        TextBoxURL.Text = "";
                        labelInfo.Content = message;
                    }
                    else
                    {
                        labelInfo.Content = message;
                        HandyControl.Controls.MessageBox.Error(message, "Ошибка!");
                    }
                });
            };

            try
            {
                await _downloadService.DownloadAsync(
                    TextBoxURL.Text,
                    settings,
                    CheckAudio.IsChecked == true,
                    CheckBoxPlaylist.IsChecked == true,
                    CheckCoder.IsChecked == true,
                    token);
            }
            catch (OperationCanceledException)
            {
                Dispatcher.Invoke(() =>
                {
                    labelInfo.Content = "Загрузка отменена";
                    ProgressBarMain.Value = 0;
                    ButDownload.IsEnabled = true;
                    ButStop.IsEnabled = false;
                });
            }
        }

        /// <summary>
        /// Останавливает текущую загрузку.
        /// </summary>
        private void ButStop_Click(object sender, RoutedEventArgs e)
        {
            _cts?.Cancel();
            ButStop.IsEnabled = false;
        }

        /// <summary>
        /// Кнопка открытия папки с видео
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
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

        /// <summary>
        /// Инициализирует приложение и подготовку необходимых переменных и папок для работы с видео.
        /// </summary>
        private void InitApp()
        {

            string videoPath = @".\MyVideos\";
            string logPath = @".\log\";

            string[] formats = new string[] { "", "AVI", "MKV", "MP4", "WEBM" };

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

        /// <summary>
        /// Проверяет наличие обновлений для приложения yt-dlp на GitHub
        /// </summary>
        private async void CheckUpdateAsync()
        {
            if (!await CheckForInternetConnection())
                return;

            var info = await _updateService!.CheckForUpdateAsync();

            if (!info.NeedsUpdate)
                return;

            bool confirmed;
            if (info.CurrentVersion == string.Empty)
            {
                HandyControl.Controls.MessageBox.Error(
                    "При проверке обновлений не был найден файл yt-dlp!\nБудет загружена последняя версия.",
                    "Ошибка!");
                confirmed = true;
            }
            else
            {
                confirmed = HandyControl.Controls.MessageBox.Ask(
                    $"Текущая версия: {info.CurrentVersion}\nПоследняя версия: {info.LatestVersion}\nПодтвердите начало обновления.",
                    "Доступна новая версия yt-dlp!") == MessageBoxResult.OK;
            }

            if (confirmed && info.DownloadUrl != null)
            {
                ButDownload.IsEnabled = false;
                await _updateService.DownloadUpdateAsync(info.DownloadUrl);
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

        private void ButQueue_Click(object sender, RoutedEventArgs e)
        {
            var queueWindow = new QueueWindow.QueueWindow(settings, CheckAudio.IsChecked == true,
                CheckBoxPlaylist.IsChecked == true, CheckCoder.IsChecked == true);
            queueWindow.Owner = this;
            queueWindow.ShowDialog();
        }

        private void ButTheme_Click(object sender, RoutedEventArgs e)
        {
            bool isDark = settings.Theme != "Light";
            ApplyTheme(isDark);
            settings = settings with { Theme = isDark ? "Dark" : "Light" };
            settings.Save();
        }

        private void ApplyTheme(bool isDark)
        {
            var skin = isDark ? SkinType.Dark : SkinType.Default;
            foreach (var dict in Application.Current.Resources.MergedDictionaries)
            {
                if (dict is HandyControl.Themes.StandaloneTheme theme)
                {
                    theme.Skin = skin;
                    break;
                }
            }
        }

        private void ButLang_Click(object sender, RoutedEventArgs e)
        {
            bool isEnglish = settings.Language != "en";
            settings = settings with { Language = isEnglish ? "en" : "ru" };
            settings.Save();

            var culture = isEnglish ? new CultureInfo("en") : new CultureInfo("ru");
            Thread.CurrentThread.CurrentUICulture = culture;
            Thread.CurrentThread.CurrentCulture = culture;

            HandyControl.Controls.MessageBox.Info(
                isEnglish ? "Language changed. Restart the application for full effect." :
                           "Язык изменён. Для полного применения перезапустите приложение.",
                "Language / Язык");
        }

        /// <summary>
        /// Этот код создает анимацию изменения цвета фона TextBoxURL
        /// </summary>
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


        /// <summary>
        /// Проверка подключения к интернету.
        /// </summary>
        /// <param name="timeoutMs">Время ожидания сети</param>
        /// <param name="url">Ссылка для проверки подключения</param>
        /// <returns></returns>
        public async Task<bool> CheckForInternetConnection(int timeoutMs = 1000, string url = null)
        {
            try
            {
                url ??= CultureInfo.InstalledUICulture switch
                {
                    { Name: var n } when n.StartsWith("ru") =>
                        "https://ya.ru/",
                    _ =>
                        "http://www.gstatic.com/generate_204",
                };

                using var httpClient = new HttpClient { Timeout = TimeSpan.FromMilliseconds(timeoutMs) };
                using var response = await httpClient.GetAsync(url).ConfigureAwait(false);
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

    }
}
