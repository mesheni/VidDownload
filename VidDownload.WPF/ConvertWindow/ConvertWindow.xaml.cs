using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using VidDownload.WPF.Control;
using Xabe.FFmpeg;

namespace VidDownload.WPF.ConvertWindow
{
    /// <summary>
    /// Логика взаимодействия для ConvertWindow.xaml
    /// </summary>
    public partial class ConvertWindow : System.Windows.Window
    {
        private string fileName = String.Empty;
        private FFmpegAction? _ffmpegAction;

        public ConvertWindow()
        {
            InitializeComponent();
            InitializeFFmpegAction();
        }

        private void InitializeFFmpegAction()
        {
            _ffmpegAction = new FFmpegAction(
                onProgress: (percent, message) =>
                {
                    Dispatcher.Invoke(() =>
                    {
                        labelInfoFFmpeg.Content = message;
                        ProgressBarFFmpeg.Value = percent;
                    });
                },
                onError: (errorMessage) =>
                {
                    Dispatcher.Invoke(() =>
                    {
                        MessageBox.Show(this, errorMessage, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                        labelInfoFFmpeg.Content = string.Empty;
                        ProgressBarFFmpeg.Value = 0;
                    });
                },
                onCompleted: () =>
                {
                    Dispatcher.Invoke(() =>
                    {
                        labelInfoFFmpeg.Content = "Конвертация завершена!";
                        ProgressBarFFmpeg.Value = 100;
                    });
                }
            );
        }

        private async void ButConvert_Click(object sender, RoutedEventArgs e)
        {
            // Валидация входного файла
            if (string.IsNullOrEmpty(fileName) || !File.Exists(fileName))
            {
                MessageBox.Show(this, "Пожалуйста, выберите видеофайл для конвертации.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Получение выбранного формата из ComboBox
            string outputFormat = "mp4"; // формат по умолчанию
            if (ComboFormat.SelectedItem is System.Windows.Controls.ComboBoxItem selectedFormat && selectedFormat.Content != null)
            {
                outputFormat = selectedFormat.Content.ToString()?.ToLower() ?? "mp4";
            }

            // Формирование пути к выходному файлу
            string inputExtension = Path.GetExtension(fileName);
            string outputFileName = Path.GetFileNameWithoutExtension(fileName) + "." + outputFormat;
            string outputDirectory = Path.GetDirectoryName(fileName) ?? Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            string outputPath = Path.Combine(outputDirectory, outputFileName);

            // Проверка на существование выходного файла и запрос на перезапись
            if (File.Exists(outputPath))
            {
                var result = MessageBox.Show(
                    this,
                    $"Файл \"{outputFileName}\" уже существует. Перезаписать?",
                    "Подтверждение",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result != MessageBoxResult.Yes)
                {
                    return;
                }
            }

            // Блокировка интерфейса на время конвертации
            ButConvert.IsEnabled = false;
            butChoiseVideo.IsEnabled = false;
            ComboFormat.IsEnabled = false;

            try
            {
                // Использование NVENC если доступно (можно добавить настройку в UI)
                bool useNVENC = false;

                // Запуск конвертации
                var resultPath = await _ffmpegAction!.ConvertVideoAsync(fileName, outputPath, outputFormat, useNVENC).ConfigureAwait(false);

                if (resultPath != null)
                {
                    Dispatcher.Invoke(() =>
                    {
                        MessageBox.Show(this, $"Конвертация успешно завершена!\nФайл сохранён: {resultPath}", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                    });
                }
            }
            catch (Exception ex)
            {
                Dispatcher.Invoke(() =>
                {
                    MessageBox.Show(this, $"Произошла ошибка при конвертации: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    labelInfoFFmpeg.Content = string.Empty;
                    ProgressBarFFmpeg.Value = 0;
                });
            }
            finally
            {
                // Разблокировка интерфейса
                Dispatcher.Invoke(() =>
                {
                    ButConvert.IsEnabled = true;
                    butChoiseVideo.IsEnabled = true;
                    ComboFormat.IsEnabled = true;
                });
            }
        }

        private void ButChoiseVideo_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new()
            {
                Filter = "Video files (*.mp4;*.avi;*.wmv;*.mkv;*.mov)|*.mp4;*.avi;*.wmv;*.mkv;*.mov|All files (*.*)|*.*",
                Title = "Выберите видеофайл для конвертации"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                LabelFileName.Text = openFileDialog.FileName;
                fileName = openFileDialog.FileName;
                
                // Автоматический выбор формата на основе расширения исходного файла (опционально)
                // string currentExt = Path.GetExtension(fileName).TrimStart('.').ToUpper();
                // Можно добавить логику для предложения формата
            }
        }
    }
}
