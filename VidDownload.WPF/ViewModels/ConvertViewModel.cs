using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using VidDownload.WPF.Control;
using VidDownload.WPF.Services;
using VidDownload.WPF.Resources;
using VidDownload.WPF.ViewModels.Base;

namespace VidDownload.WPF.ViewModels
{
    public partial class ConvertViewModel : ViewModelBase
    {
        private readonly FFmpegAction _ffmpegAction;
        private readonly IMessageService _messageService;
        private readonly IDialogService _dialogService;
        private readonly ISettingsService _settingsService;
        private readonly LocalizedStrings _loc;
        private CancellationTokenSource? _cts;

        public LocalizedStrings LocalizedStrings => _loc;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(ConvertCommand))]
        private string _filePath = string.Empty;

        [ObservableProperty]
        private string _selectedFormat = "MP4";

        [ObservableProperty]
        private string _selectedVideoCodec = "libx264";

        [ObservableProperty]
        private string _selectedAudioCodec = "aac";

        [ObservableProperty]
        private string _selectedHardwareEncoder = string.Empty;

        [ObservableProperty]
        private int _crf = 23;

        [ObservableProperty]
        private int _videoBitrate;

        [ObservableProperty]
        private int _audioBitrate;

        [ObservableProperty]
        private string _selectedPreset = "medium";

        [ObservableProperty]
        private string _outputDirectory = string.Empty;

        [ObservableProperty]
        private string _outputFileName = string.Empty;

        [ObservableProperty]
        private string _statusMessage = string.Empty;

        [ObservableProperty]
        private int _progressPercent;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(ConvertCommand))]
        [NotifyCanExecuteChangedFor(nameof(BrowseFileCommand))]
        [NotifyCanExecuteChangedFor(nameof(BrowseOutputDirCommand))]
        [NotifyCanExecuteChangedFor(nameof(CancelConvertCommand))]
        private bool _isConverting;

        [ObservableProperty]
        private bool _isCancellable;

        [ObservableProperty]
        private string _ffmpegCommandPreview = string.Empty;

        public ObservableCollection<string> Formats { get; }

        public ObservableCollection<string> VideoCodecs { get; } = new();

        public ObservableCollection<string> AudioCodecs { get; } = new();

        public ObservableCollection<string> HardwareEncoders { get; } = new()
        {
            "None", "NVENC", "AMF", "QSV"
        };

        public ObservableCollection<string> Presets { get; } = new()
        {
            "ultrafast", "superfast", "veryfast", "faster", "fast",
            "medium", "slow", "slower", "veryslow"
        };

        public ConvertViewModel(
            IMessageService messageService,
            IDialogService dialogService,
            ISettingsService settingsService,
            LocalizedStrings localizedStrings)
        {
            _ffmpegAction = new FFmpegAction();
            _messageService = messageService;
            _dialogService = dialogService;
            _settingsService = settingsService;
            _loc = localizedStrings;

            Formats = new ObservableCollection<string>(ConversionOptions.AllFormats);

            SelectedFormat = "MP4";
            RefreshCodecLists();
            RefreshCommandPreview();
        }

        public async Task InitializeAsync()
        {
            var settings = await _settingsService.LoadAsync().ConfigureAwait(true);
            SelectedFormat = string.IsNullOrEmpty(settings.ConvertOutputFormat) ? "MP4" : settings.ConvertOutputFormat;
            SelectedVideoCodec = string.IsNullOrEmpty(settings.ConvertVideoCodec) ? "libx264" : settings.ConvertVideoCodec;
            SelectedAudioCodec = string.IsNullOrEmpty(settings.ConvertAudioCodec) ? "aac" : settings.ConvertAudioCodec;
            SelectedHardwareEncoder = string.IsNullOrEmpty(settings.ConvertHardwareEncoder) ? string.Empty : settings.ConvertHardwareEncoder;
            Crf = settings.ConvertCrf > 0 ? settings.ConvertCrf : 23;
            VideoBitrate = settings.ConvertVideoBitrate;
            AudioBitrate = settings.ConvertAudioBitrate;
            SelectedPreset = string.IsNullOrEmpty(settings.ConvertPreset) ? "medium" : settings.ConvertPreset;
            OutputDirectory = string.IsNullOrEmpty(settings.ConvertOutputDir) ? string.Empty : settings.ConvertOutputDir;

            RefreshCodecLists();
            RefreshCommandPreview();
        }

        private bool CanConvert() => !IsConverting && !string.IsNullOrEmpty(FilePath) && File.Exists(FilePath);

        private bool CanBrowseFile() => !IsConverting;

        private bool CanBrowseOutputDir() => !IsConverting;

        private bool CanCancelConvert() => IsCancellable && IsConverting;

        partial void OnSelectedFormatChanged(string value)
        {
            RefreshCodecLists();
            RefreshCommandPreview();
        }

        partial void OnSelectedHardwareEncoderChanged(string value)
        {
            RefreshCodecLists();
            RefreshCommandPreview();
        }

        partial void OnSelectedVideoCodecChanged(string value) => RefreshCommandPreview();
        partial void OnSelectedAudioCodecChanged(string value) => RefreshCommandPreview();
        partial void OnCrfChanged(int value) => RefreshCommandPreview();
        partial void OnVideoBitrateChanged(int value) => RefreshCommandPreview();
        partial void OnAudioBitrateChanged(int value) => RefreshCommandPreview();
        partial void OnSelectedPresetChanged(string value) => RefreshCommandPreview();
        partial void OnFilePathChanged(string value) => RefreshCommandPreview();
        partial void OnOutputDirectoryChanged(string value) => RefreshCommandPreview();
        partial void OnOutputFileNameChanged(string value) => RefreshCommandPreview();

        private void RefreshCodecLists()
        {
            string format = SelectedFormat ?? "MP4";
            string hwEncoder = MapHardwareEncoderDisplayToKey(SelectedHardwareEncoder ?? string.Empty);

            var videoList = new List<string>();
            var candidates = ConversionOptions.GetVideoCodecsForHardwareEncoder(hwEncoder);
            var formatCodecs = ConversionOptions.GetVideoCodecsForFormat(format);
            var formatSet = new HashSet<string>(formatCodecs, StringComparer.OrdinalIgnoreCase);

            foreach (var codec in candidates)
            {
                if (formatSet.Contains(codec))
                    videoList.Add(codec);
            }

            if (videoList.Count == 0)
                videoList.Add("libx264");

            VideoCodecs.Clear();
            foreach (var c in videoList)
                VideoCodecs.Add(c);

            if (!VideoCodecs.Contains(SelectedVideoCodec ?? string.Empty))
                SelectedVideoCodec = videoList.FirstOrDefault() ?? "libx264";

            var audioFormatCodecs = ConversionOptions.GetAudioCodecsForFormat(format);
            AudioCodecs.Clear();
            foreach (var c in audioFormatCodecs)
                AudioCodecs.Add(c);

            if (!AudioCodecs.Contains(SelectedAudioCodec ?? string.Empty))
                SelectedAudioCodec = audioFormatCodecs.FirstOrDefault() ?? "aac";
        }

        private static string MapHardwareEncoderDisplayToKey(string display)
        {
            return (display ?? string.Empty).ToLower() switch
            {
                "nvenc" => "nvenc",
                "nv" => "nvenc",
                "amf" => "amf",
                "qsv" => "qsv",
                "none" => string.Empty,
                "" => string.Empty,
                _ => "nvenc"
            };
        }

        private void RefreshCommandPreview()
        {
            if (string.IsNullOrEmpty(FilePath) || !File.Exists(FilePath))
            {
                FfmpegCommandPreview = string.Empty;
                return;
            }

            var options = BuildConversionOptions();
            FfmpegCommandPreview = FFmpegAction.BuildCommandPreview(options);
        }

        private ConversionOptions BuildConversionOptions()
        {
            string format = (SelectedFormat ?? "MP4").ToLower();
            string outputDir = string.IsNullOrEmpty(OutputDirectory)
                ? Path.GetDirectoryName(FilePath) ?? Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)
                : OutputDirectory;

            string fileName = !string.IsNullOrEmpty(OutputFileName)
                ? OutputFileName
                : Path.GetFileNameWithoutExtension(FilePath) + "." + format;

            if (!fileName.EndsWith("." + format, StringComparison.OrdinalIgnoreCase))
                fileName += "." + format;

            string outputPath = Path.Combine(outputDir, fileName);

            return new ConversionOptions
            {
                InputPath = FilePath,
                OutputPath = outputPath,
                OutputFormat = format,
                VideoCodec = SelectedVideoCodec ?? "libx264",
                AudioCodec = SelectedAudioCodec ?? "aac",
                HardwareEncoder = MapHardwareEncoderDisplayToKey(SelectedHardwareEncoder ?? string.Empty),
                Crf = Crf > 0 ? Crf : null,
                VideoBitrate = VideoBitrate > 0 ? VideoBitrate : null,
                AudioBitrate = AudioBitrate > 0 ? AudioBitrate : null,
                Preset = SelectedPreset ?? "medium"
            };
        }

        [RelayCommand]
        private async Task ConvertAsync()
        {
            if (string.IsNullOrEmpty(FilePath) || !File.Exists(FilePath))
            {
                _messageService.Warning(_loc["SelectFileForConversion"], _loc["ErrorTitle"]);
                return;
            }

            var options = BuildConversionOptions();

            if (!Directory.Exists(Path.GetDirectoryName(options.OutputPath) ?? string.Empty))
            {
                try { Directory.CreateDirectory(Path.GetDirectoryName(options.OutputPath)!); }
                catch
                {
                    _messageService.Error(
                        string.Format(_loc["NoSaveFolderAccess"], options.OutputPath),
                        _loc["ErrorTitle"]);
                    return;
                }
            }

            if (File.Exists(options.OutputPath))
            {
                if (!await _dialogService.AskAsync(
                    string.Format(_loc["FileExistsOverwrite"], Path.GetFileName(options.OutputPath)),
                    _loc["ConfirmationTitle"]))
                    return;
            }

            IsConverting = true;
            IsCancellable = true;
            _cts = new CancellationTokenSource();

            try
            {
                var progress = new Progress<DownloadProgress>(p =>
                {
                    StatusMessage = p.StatusMessage;
                    ProgressPercent = p.Percent;
                });

                var resultPath = await _ffmpegAction.ConvertVideoAsync(options, progress, _cts.Token);

                if (resultPath != null)
                {
                    _messageService.Info(string.Format(_loc["ConversionSuccess"], resultPath), _loc["SuccessTitle"]);
                    await SaveSettingsAsync();
                }
            }
            catch (OperationCanceledException)
            {
                StatusMessage = _loc["DownloadCancelled"];
                ProgressPercent = 0;
            }
            catch (Exception ex)
            {
                _messageService.Error(string.Format(_loc["ConversionError"], ex.Message), _loc["ErrorTitle"]);
                StatusMessage = string.Empty;
                ProgressPercent = 0;
            }
            finally
            {
                IsCancellable = false;
                IsConverting = false;
                _cts?.Dispose();
                _cts = null;
            }
        }

        [RelayCommand]
        private void CancelConvert()
        {
            if (_cts != null && IsCancellable)
            {
                _cts.Cancel();
                IsCancellable = false;
            }
        }

        [RelayCommand]
        private void BrowseFile()
        {
            OpenFileDialog openFileDialog = new()
            {
                Filter = _loc["VideoFileFilter"],
                Title = _loc["SelectVideoFileDialogTitle"]
            };

            if (openFileDialog.ShowDialog() == true)
            {
                FilePath = openFileDialog.FileName;
                RefreshCommandPreview();
            }
        }

        [RelayCommand]
        private void BrowseOutputDir()
        {
            var dialog = new OpenFolderDialog
            {
                Title = _loc["OutputFolderDialogTitle"],
                InitialDirectory = string.IsNullOrEmpty(OutputDirectory)
                    ? Environment.GetFolderPath(Environment.SpecialFolder.MyVideos)
                    : OutputDirectory
            };

            if (dialog.ShowDialog() == true)
            {
                OutputDirectory = dialog.FolderName;
                RefreshCommandPreview();
            }
        }

        private async Task SaveSettingsAsync()
        {
            var settings = await _settingsService.LoadAsync().ConfigureAwait(true);
            settings.ConvertOutputFormat = SelectedFormat ?? "MP4";
            settings.ConvertVideoCodec = SelectedVideoCodec ?? "libx264";
            settings.ConvertAudioCodec = SelectedAudioCodec ?? "aac";
            settings.ConvertHardwareEncoder = MapHardwareEncoderDisplayToKey(SelectedHardwareEncoder ?? string.Empty);
            settings.ConvertCrf = Crf;
            settings.ConvertVideoBitrate = VideoBitrate;
            settings.ConvertAudioBitrate = AudioBitrate;
            settings.ConvertPreset = SelectedPreset ?? "medium";
            settings.ConvertOutputDir = OutputDirectory ?? string.Empty;
            await _settingsService.SaveAsync(settings).ConfigureAwait(true);
        }
    }
}
