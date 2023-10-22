﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using VidDownload.WPF.Control;
using VidDownload.WPF.Help;
using Xabe.FFmpeg;
using static System.Reflection.AssemblyKeyFileAttribute;

namespace VidDownload.WPF
{
    public partial class MainWindow : Window
    {
        // Переменные для сборки команды
        private string res = "2160";
        private static List<string> codecList = new List<string>();
        private string codec = "av01";
        private string acodec = "mp3";
        private string format = "";

        public MainWindow()
        {
            InitializeComponent();
            InitApp();
        }

        // Кнопка загрузки видео
        private async void  ButDownload_Click(object sender, RoutedEventArgs e)
        {
            // Проверка на пустое поле ссылки
            if (TextBoxURL.Text.Length == 0)
            {
                MessageBox.Show("Пустое поле ссылки!", "Ошибка!",
                    MessageBoxButton.OK, MessageBoxImage.Error);
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

        // Функция загрузки видео
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
                        proc.StartInfo.Arguments = Command.LoadVideo(TextBoxURL.Text, codec, res, CheckBoxPlaylist.IsChecked, CheckCoder.IsChecked, format);
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
            MessageBox.Show("Перекодирование видео может потребовать длительного времени. Лучше воспользуйтесь сторонними конвертерами.", "Предупреждение",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
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
    }
}
