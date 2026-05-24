using System.Collections.ObjectModel;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using VidDownload.WPF.Control;
using VidDownload.WPF.Services;
using VidDownload.WPF.ViewModels.Base;

namespace VidDownload.WPF.ViewModels
{
    public partial class ConvertViewModel : ViewModelBase
    {
        private readonly FFmpegAction _ffmpegAction;
        private readonly IMessageService _messageService;
        private readonly IDialogService _dialogService;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(ConvertCommand))]
        private string _filePath = string.Empty;

        [ObservableProperty]
        private string _selectedFormat = "MP4";

        [ObservableProperty]
        private string _statusMessage = string.Empty;

        [ObservableProperty]
        private int _progressPercent;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(ConvertCommand))]
        [NotifyCanExecuteChangedFor(nameof(BrowseFileCommand))]
        private bool _isConverting;

        public ObservableCollection<string> Formats { get; } = new()
        {
            "", "AVI", "MP4", "MKV", "MOV"
        };

        public ConvertViewModel(IMessageService messageService, IDialogService dialogService)
        {
            _ffmpegAction = new FFmpegAction();
            _messageService = messageService;
            _dialogService = dialogService;
        }

        private bool CanConvert() => !IsConverting && !string.IsNullOrEmpty(FilePath) && File.Exists(FilePath);

        private bool CanBrowseFile() => !IsConverting;

        [RelayCommand]
        private async Task ConvertAsync()
        {
            if (string.IsNullOrEmpty(FilePath) || !File.Exists(FilePath))
            {
                _messageService.Warning("Пожалуйста, выберите видеофайл для конвертации.", "Ошибка");
                return;
            }

            string outputFormat = string.IsNullOrEmpty(SelectedFormat) ? "mp4" : SelectedFormat.ToLower();
            string outputFileName = Path.GetFileNameWithoutExtension(FilePath) + "." + outputFormat;
            string outputDirectory = Path.GetDirectoryName(FilePath) ?? Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            string outputPath = Path.Combine(outputDirectory, outputFileName);

            if (File.Exists(outputPath))
            {
                if (!await _dialogService.AskAsync($"Файл \"{outputFileName}\" уже существует. Перезаписать?", "Подтверждение"))
                    return;
            }

            IsConverting = true;

            try
            {
                var progress = new Progress<DownloadProgress>(p =>
                {
                    StatusMessage = p.StatusMessage;
                    ProgressPercent = p.Percent;
                });

                var resultPath = await _ffmpegAction.ConvertVideoAsync(FilePath, outputPath, outputFormat, false, progress);

                if (resultPath != null)
                {
                    _messageService.Info($"Конвертация успешно завершена!\nФайл сохранён: {resultPath}", "Успех");
                }
            }
            catch (Exception ex)
            {
                _messageService.Error($"Произошла ошибка при конвертации: {ex.Message}", "Ошибка");
                StatusMessage = string.Empty;
                ProgressPercent = 0;
            }
            finally
            {
                IsConverting = false;
            }
        }

        [RelayCommand]
        private void BrowseFile()
        {
            OpenFileDialog openFileDialog = new()
            {
                Filter = "Video files (*.mp4;*.avi;*.wmv;*.mkv;*.mov)|*.mp4;*.avi;*.wmv;*.mkv;*.mov|All files (*.*)|*.*",
                Title = "Выберите видеофайл для конвертации"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                FilePath = openFileDialog.FileName;
            }
        }
    }
}
