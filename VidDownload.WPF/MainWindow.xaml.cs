using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Media.Animation;
using VidDownload.WPF.Control;
using VidDownload.WPF.Help;
using HandyControl.Data;

namespace VidDownload.WPF
{
    public partial class MainWindow : System.Windows.Window
    {
        private List<string> codecList = new();
        Settings settings = new();
        private UpdateService? _updateService;
        private DownloadService? _downloadService;
        private CancellationTokenSource? _cts;

        // ===== Очередь =====
        private readonly ObservableCollection<QueueItem> _queueItems = new();
        private CancellationTokenSource? _queueCts;
        private bool _isQueueProcessing;

        public MainWindow()
        {
            InitializeComponent();
            InitApp();
            LoadSavedSettings();
            ListViewQueue.ItemsSource = _queueItems;
            UpdateQueueStatus();

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

        // ===== НАСТРОЙКИ =====

        private void LoadSavedSettings()
        {
            settings = Settings.Load();

            SelectComboItem(ComboRes, settings.Resolution);
            SelectComboItem(ComboCodec, settings.VideoCodec);
            SelectComboItem(ComboAudio, settings.AudioCodec);
            SelectComboItem(ComboFormat, settings.Format);

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

        private static void SelectComboItem(ComboBox combo, string value)
        {
            if (string.IsNullOrEmpty(value)) return;
            foreach (var obj in combo.Items)
            {
                string itemText = obj is ComboBoxItem cbi ? cbi.Content?.ToString() : obj.ToString();
                if (string.Equals(itemText, value, StringComparison.OrdinalIgnoreCase))
                {
                    combo.SelectedItem = obj;
                    return;
                }
            }
        }

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

        // ===== СКАЧИВАНИЕ =====

        private async void ButDownload_Click(object sender, RoutedEventArgs e)
        {
            if (TextBoxURL.Text.Length == 0)
            {
                await Task.Run(() => Dispatcher.Invoke(() => TextBoxAnimation())).ConfigureAwait(false);
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
                        SelectComboItem(ComboCodec, settings.VideoCodec);
                        SelectComboItem(ComboRes, settings.Resolution);
                        SelectComboItem(ComboAudio, settings.AudioCodec);
                        SelectComboItem(ComboFormat, settings.Format);
                        TextBoxURL.Text = "";
                        labelInfo.Content = message;
                    }
                    else
                    {
                        labelInfo.Content = message;
                        // Не показывать ошибку если загрузка отменена пользователем
                        if (message != "Загрузка отменена")
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

        private void ButStop_Click(object sender, RoutedEventArgs e)
        {
            if (_cts != null)
            {
                _cts.Cancel();
                _cts = null;
            }
            ButStop.IsEnabled = false;
        }

        // ===== ОЧЕРЕДЬ =====

        private void TextBoxQueueUrl_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Enter)
            {
                AddUrlsToQueue(TextBoxQueueUrl.Text.Trim());
                TextBoxQueueUrl.Text = string.Empty;
                e.Handled = true;
            }
        }

        private void ButAddToQueue_Click(object sender, RoutedEventArgs e)
        {
            AddUrlsToQueue(TextBoxQueueUrl.Text.Trim());
            TextBoxQueueUrl.Text = string.Empty;
        }

        private void AddUrlsToQueue(string text)
        {
            if (string.IsNullOrEmpty(text)) return;

            var urls = text.Split(new[] { '\n', ';', '\r' }, StringSplitOptions.RemoveEmptyEntries)
                           .Select(u => u.Trim())
                           .Where(u => u.Length > 0)
                           .ToArray();

            foreach (var url in urls)
            {
                var item = new QueueItem(url);
                _queueItems.Add(item);
                _ = FetchVideoTitleAsync(item);
            }
            UpdateQueueStatus();
        }

        private async Task FetchVideoTitleAsync(QueueItem item)
        {
            try
            {
                if (!File.Exists(@".\yt-dlp.exe"))
                {
                    item.Title = item.Url;
                    return;
                }

                var proc = new Process();
                proc.StartInfo.FileName = @".\yt-dlp.exe";
                proc.StartInfo.Arguments = $"--no-warnings --print title --print thumbnail \"{item.Url}\"";
                proc.StartInfo.UseShellExecute = false;
                proc.StartInfo.RedirectStandardOutput = true;
                proc.StartInfo.RedirectStandardError = true;
                proc.StartInfo.CreateNoWindow = true;

                proc.Start();
                
                string output = await proc.StandardOutput.ReadToEndAsync().ConfigureAwait(false);
                string error = await proc.StandardError.ReadToEndAsync().ConfigureAwait(false);
                
                await Dispatcher.InvokeAsync(() =>
                {
                    Debug.WriteLine($"yt-dlp stdout: {output}");
                    if (!string.IsNullOrEmpty(error))
                        Debug.WriteLine($"yt-dlp stderr: {error}");
                });

                await proc.WaitForExitAsync().ConfigureAwait(false);

                var lines = output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                if (lines.Length > 0 && !string.IsNullOrEmpty(lines[0]))
                {
                    string title = lines[0].Trim();
                    if (title.Length > 120) title = title[..117] + "...";
                    await Dispatcher.InvokeAsync(() => item.Title = title);
                }

                if (lines.Length > 1 && !string.IsNullOrEmpty(lines[1]))
                {
                    await Dispatcher.InvokeAsync(() => item.ThumbnailUrl = lines[1].Trim());
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to fetch video title: {ex.Message}");
                if (string.IsNullOrEmpty(item.Title) || item.Title == item.Url)
                    item.Title = item.Url;
            }
        }

        private void ButRemoveQueueItem_Click(object sender, RoutedEventArgs e)
        {
            if (_isQueueProcessing) return;
            if (sender is Button btn && btn.Tag is QueueItem item)
            {
                _queueItems.Remove(item);
                UpdateQueueStatus();
            }
        }

        private void ButClearQueue_Click(object sender, RoutedEventArgs e)
        {
            if (_isQueueProcessing) return;
            _queueItems.Clear();
            UpdateQueueStatus();
        }

        private async void ButStartQueue_Click(object sender, RoutedEventArgs e)
        {
            if (_isQueueProcessing || _queueItems.Count == 0) return;

            SaveCurrentSettings();

            _isQueueProcessing = true;
            _queueCts = new CancellationTokenSource();
            var token = _queueCts.Token;

            ButStartQueue.IsEnabled = false;
            ButStopQueue.IsEnabled = true;
            ButAddToQueue.IsEnabled = false;
            ButClearQueue.IsEnabled = false;

            try
            {
                for (int i = 0; i < _queueItems.Count; i++)
                {
                    if (token.IsCancellationRequested) break;

                    var item = _queueItems[i];
                    item.Status = "Загрузка...";
                    TextQueueStatus.Text = $"Загрузка {i + 1} из {_queueItems.Count}";

                    var downloadService = new DownloadService();
                    var tcs = new TaskCompletionSource<bool>();

                    downloadService.ProgressChanged += (progress) =>
                    {
                        Dispatcher.Invoke(() => item.Status = $"Загрузка... {progress}%");
                    };

                    downloadService.DownloadCompleted += (success, message) =>
                    {
                        Dispatcher.Invoke(() =>
                        {
                            item.Status = success ? "✓ Готово" : $"✗ {message}";
                        });
                        tcs.TrySetResult(success);
                    };

                    try
                    {
                        var downloadTask = downloadService.DownloadAsync(
                            item.Url, settings,
                            CheckAudio.IsChecked == true,
                            CheckBoxPlaylist.IsChecked == true,
                            CheckCoder.IsChecked == true,
                            token);

                        await Task.WhenAny(downloadTask, tcs.Task).ConfigureAwait(true);
                    }
                    catch (OperationCanceledException)
                    {
                        Dispatcher.Invoke(() => item.Status = "✗ Отменено");
                        break;
                    }
                    catch (Exception ex)
                    {
                        Dispatcher.Invoke(() => item.Status = $"✗ {ex.Message}");
                    }
                }
            }
            finally
            {
                _isQueueProcessing = false;
                _queueCts?.Dispose();
                _queueCts = null;

                Dispatcher.Invoke(() =>
                {
                    ButStartQueue.IsEnabled = true;
                    ButStopQueue.IsEnabled = false;
                    ButAddToQueue.IsEnabled = true;
                    ButClearQueue.IsEnabled = true;

                    var allDone = _queueItems.All(i => i.Status.StartsWith("✓", StringComparison.Ordinal));
                    TextQueueStatus.Text = allDone
                        ? "Все загрузки завершены"
                        : "Обработка очереди завершена";
                });
            }
        }

        private void ButStopQueue_Click(object sender, RoutedEventArgs e)
        {
            _queueCts?.Cancel();
            ButStopQueue.IsEnabled = false;
        }

        private void UpdateQueueStatus()
        {
            TextQueueStatus.Text = _queueItems.Count > 0
                ? $"В очереди: {_queueItems.Count} видео"
                : "Очередь пуста";
        }

        // ===== ПРОЧИЕ КНОПКИ =====

        private void ButOpenFolder_Click(object sender, RoutedEventArgs e)
        {
            string path = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory + @"MyVideos\");
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);
            string argument = "/open, \"" + path;
            System.Diagnostics.Process.Start("explorer.exe", argument);
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

        // ===== ТЕМА =====

        private void ButTheme_Click(object sender, RoutedEventArgs e)
        {
            bool makeDark = settings.Theme != "Light";
            ApplyTheme(makeDark);
            settings = settings with { Theme = makeDark ? "Dark" : "Light" };
            settings.Save();
        }

        private void ApplyTheme(bool isDark)
        {
            var skinUri = isDark
                ? "pack://application:,,,/HandyControl;component/Themes/SkinDark.xaml"
                : "pack://application:,,,/HandyControl;component/Themes/SkinDefault.xaml";

            int skinIndex = -1;
            for (int i = 0; i < Application.Current.Resources.MergedDictionaries.Count; i++)
            {
                var src = Application.Current.Resources.MergedDictionaries[i].Source?.OriginalString ?? "";
                if (src.Contains("Skin"))
                {
                    skinIndex = i;
                    break;
                }
            }

            if (skinIndex >= 0)
            {
                Application.Current.Resources.MergedDictionaries.RemoveAt(skinIndex);
                Application.Current.Resources.MergedDictionaries.Insert(skinIndex,
                    new ResourceDictionary { Source = new Uri(skinUri, UriKind.Absolute) });
            }
            else
            {
                // Если скин не найден, добавляем в начало (индекс 0)
                Application.Current.Resources.MergedDictionaries.Insert(0,
                    new ResourceDictionary { Source = new Uri(skinUri, UriKind.Absolute) });
            }
        }

        // ===== ЯЗЫК =====

        private void ButLang_Click(object sender, RoutedEventArgs e)
        {
            bool makeEnglish = settings.Language != "en";
            settings = settings with { Language = makeEnglish ? "en" : "ru" };
            settings.Save();

            var result = HandyControl.Controls.MessageBox.Show(
                makeEnglish
                    ? "Language changed to English. Restart now?"
                    : "Язык изменён на русский. Перезапустить сейчас?",
                "Language / Язык",
                MessageBoxButton.YesNo,
                MessageBoxImage.Information);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    System.Diagnostics.Process.Start(
                        new System.Diagnostics.ProcessStartInfo
                        {
                            FileName = System.Reflection.Assembly.GetEntryAssembly().Location,
                            UseShellExecute = true
                        });
                    Application.Current.Shutdown();
                }
                catch
                {
                    HandyControl.Controls.MessageBox.Info(
                        makeEnglish
                            ? "Please restart the application manually."
                            : "Пожалуйста, перезапустите приложение вручную.",
                        "Restart Required");
                }
            }
        }

        // ===== ИНИЦИАЛИЗАЦИЯ =====

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
                Directory.CreateDirectory(videoPath);
            if (!Directory.Exists(logPath))
                Directory.CreateDirectory(logPath);

            foreach (var i in ComboCodec.Items)
            {
                if (i is ComboBoxItem cbi && cbi.Content != null)
                    codecList.Add(cbi.Content.ToString()!);
            }
        }

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

        // ===== CHECKBOX HANDLERS =====

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

        // ===== АНИМАЦИЯ =====

        private void TextBoxAnimation()
        {
            ColorAnimation colorAnimation = new()
            {
                From = Colors.White,
                To = (Color)ColorConverter.ConvertFromString("#ff4f4f"),
                AutoReverse = true,
                Duration = TimeSpan.FromSeconds(0.5f),
                RepeatBehavior = new RepeatBehavior(2)
            };

            TextBoxURL.Background = new SolidColorBrush(Colors.White);
            TextBoxURL.Background.BeginAnimation(SolidColorBrush.ColorProperty, colorAnimation);
        }

        // ===== ИНТЕРНЕТ =====

        public async Task<bool> CheckForInternetConnection(int timeoutMs = 1000, string url = null)
        {
            try
            {
                url ??= CultureInfo.InstalledUICulture switch
                {
                    { Name: var n } when n.StartsWith("ru", StringComparison.OrdinalIgnoreCase) =>
                        "https://ya.ru/",
                    _ =>
                        "http://www.gstatic.com/generate_204",
                };

                using var httpClient = new HttpClient { Timeout = TimeSpan.FromMilliseconds(timeoutMs) };
                using var response = await httpClient.GetAsync(new Uri(url)).ConfigureAwait(false);
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }
    }
}
