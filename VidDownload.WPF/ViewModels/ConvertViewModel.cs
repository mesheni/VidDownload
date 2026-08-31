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
        private readonly IFFmpegService _ffmpegService;
        private readonly LocalizedStrings _loc;
        private CancellationTokenSource? _cts;

        /// <summary>Аппаратные кодеки, подтверждённые тестовым кодированием (без GPU они не работают).</summary>
        private readonly HashSet<string> _workingHardwareCodecs = new(StringComparer.OrdinalIgnoreCase);

        /// <summary>Сохранённый в настройках кодек — восстанавливается после пробы аппаратных кодеров.</summary>
        private string? _pendingSavedVideoCodec;

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

        /// <summary>Режим «только аудио»: выходной файл — звук без видеодорожки.</summary>
        [ObservableProperty]
        private bool _isAudioOnlyMode;

        /// <summary>Пакетная конвертация списка файлов.</summary>
        [ObservableProperty]
        private bool _isBatchMode;

        [ObservableProperty]
        private string? _selectedBatchFile;

        /// <summary>Файлы пакетной конвертации.</summary>
        public ObservableCollection<string> BatchFiles { get; } = new();

        public bool ShowSingleInput => !IsBatchMode;

        public bool ShowBatchInput => IsBatchMode;

        public bool ShowOutputFileName => !IsBatchMode;

        public bool ShowVideoEncoding => !IsAudioOnlyMode;

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
            IFFmpegService ffmpegService,
            LocalizedStrings localizedStrings)
        {
            _ffmpegAction = new FFmpegAction();
            _messageService = messageService;
            _dialogService = dialogService;
            _settingsService = settingsService;
            _ffmpegService = ffmpegService;
            _loc = localizedStrings;

            Formats = new ObservableCollection<string>(ConversionOptions.AllFormats);

            SelectedFormat = "MP4";
            RefreshCodecLists();
            RefreshCommandPreview();
        }

        public async Task InitializeAsync()
        {
            var settings = await _settingsService.LoadAsync().ConfigureAwait(true);
            IsAudioOnlyMode = settings.ConvertAudioOnlyMode;
            SelectedFormat = string.IsNullOrEmpty(settings.ConvertOutputFormat) ? "MP4" : settings.ConvertOutputFormat;
            if (IsAudioOnlyMode && !Formats.Contains(SelectedFormat))
                SelectedFormat = "MP3";
            SelectedVideoCodec = string.IsNullOrEmpty(settings.ConvertVideoCodec) ? "libx264" : settings.ConvertVideoCodec;
            SelectedAudioCodec = string.IsNullOrEmpty(settings.ConvertAudioCodec) ? "aac" : settings.ConvertAudioCodec;
            SelectedHardwareEncoder = MapHardwareEncoderKeyToDisplay(settings.ConvertHardwareEncoder);
            Crf = settings.ConvertCrf > 0 ? settings.ConvertCrf : 23;
            VideoBitrate = settings.ConvertVideoBitrate;
            AudioBitrate = settings.ConvertAudioBitrate;
            SelectedPreset = string.IsNullOrEmpty(settings.ConvertPreset) ? "medium" : settings.ConvertPreset;
            OutputDirectory = string.IsNullOrEmpty(settings.ConvertOutputDir) ? string.Empty : settings.ConvertOutputDir;

            RefreshCodecLists();
            RefreshCommandPreview();

            // доступность GPU-кодеров проверяется тестовым кодированием в фоне —
            // до его завершения список кодеков строится без аппаратных
            _pendingSavedVideoCodec = settings.ConvertVideoCodec;
            _ = RefreshHardwareEncoderAvailabilityAsync();
        }

        /// <summary>Пробирует аппаратные кодеры и пересобирает списки кодировщиков/кодеков.</summary>
        private async Task RefreshHardwareEncoderAvailabilityAsync()
        {
            try
            {
                var probe = await FFmpegAction.GetOrProbeHardwareEncodersAsync().ConfigureAwait(true);

                _workingHardwareCodecs.Clear();
                foreach (var (codec, works) in probe)
                {
                    if (works)
                        _workingHardwareCodecs.Add(codec);
                }

                RebuildHardwareEncoderList();
                RefreshCodecLists();
                RefreshCommandPreview();

                // после пробы список мог расшириться — возвращаем сохранённый кодек
                if (!string.IsNullOrEmpty(_pendingSavedVideoCodec)
                    && VideoCodecs.Contains(_pendingSavedVideoCodec))
                {
                    SelectedVideoCodec = _pendingSavedVideoCodec;
                    RefreshCommandPreview();
                }

                _pendingSavedVideoCodec = null;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Hardware encoder probe failed: {ex.Message}");
            }
        }

        private void RebuildHardwareEncoderList()
        {
            string current = SelectedHardwareEncoder ?? "None";

            HardwareEncoders.Clear();
            HardwareEncoders.Add("None");
            if (HasWorkingCodec("nvenc"))
                HardwareEncoders.Add("NVENC");
            if (HasWorkingCodec("amf"))
                HardwareEncoders.Add("AMF");
            if (HasWorkingCodec("qsv"))
                HardwareEncoders.Add("QSV");

            if (!HardwareEncoders.Contains(current))
            {
                SelectedHardwareEncoder = "None";
                StatusMessage = string.Format(_loc["HwEncoderUnavailable"], current);
            }
            else
            {
                // восстановить SelectedItem комбобокса после пересборки списка
                SelectedHardwareEncoder = current;
            }
        }

        private bool HasWorkingCodec(string family) =>
            _workingHardwareCodecs.Any(c => c.EndsWith("_" + family, StringComparison.OrdinalIgnoreCase));

        private bool CanConvert() => !IsConverting &&
            (IsBatchMode
                ? BatchFiles.Count > 0
                : !string.IsNullOrEmpty(FilePath) && File.Exists(FilePath));

        private bool CanBrowseFile() => !IsConverting;

        private bool CanBrowseOutputDir() => !IsConverting;

        private bool CanCancelConvert() => IsCancellable && IsConverting;

        partial void OnSelectedFormatChanged(string value)
        {
            RefreshCodecLists();
            RefreshCommandPreview();
        }

        partial void OnIsBatchModeChanged(bool value)
        {
            OnPropertyChanged(nameof(ShowSingleInput));
            OnPropertyChanged(nameof(ShowBatchInput));
            OnPropertyChanged(nameof(ShowOutputFileName));
            ConvertCommand.NotifyCanExecuteChanged();
            RefreshCommandPreview();
        }

        partial void OnIsAudioOnlyModeChanged(bool value)
        {
            OnPropertyChanged(nameof(ShowVideoEncoding));
            RebuildFormatList();
        }

        /// <summary>Пересобирает список форматов под текущий режим (видео или аудио).</summary>
        private void RebuildFormatList()
        {
            string current = SelectedFormat ?? string.Empty;
            var source = IsAudioOnlyMode ? ConversionOptions.AudioOnlyFormats : ConversionOptions.AllFormats;

            Formats.Clear();
            foreach (var format in source)
                Formats.Add(format);

            SelectedFormat = Formats.Contains(current)
                ? current
                : (IsAudioOnlyMode ? "MP3" : "MP4");
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

            if (IsAudioOnlyMode)
            {
                // В аудио-режиме видеокодек не нужен, аудиокодек определяется форматом
                VideoCodecs.Clear();
                string audioCodec = ConversionOptions.GetAudioCodecForAudioFormat(format);
                AudioCodecs.Clear();
                AudioCodecs.Add(audioCodec);
                SelectedVideoCodec = string.Empty;
                SelectedAudioCodec = audioCodec;
                return;
            }

            var videoList = ConversionOptions.ResolveVideoCodecList(format, hwEncoder)
                .Where(codec => ConversionOptions.DetectHardwareEncoder(codec) == null
                                || _workingHardwareCodecs.Contains(codec))
                .ToList();

            if (videoList.Count == 0)
            {
                // запасной вариант — CPU-кодек, совместимый с форматом
                var formatCodecs = ConversionOptions.GetVideoCodecsForFormat(format);
                videoList.Add(formatCodecs.FirstOrDefault(c =>
                    ConversionOptions.DetectHardwareEncoder(c) == null) ?? "libx264");
            }

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
                // неизвестное значение не должно молча включать аппаратный кодировщик
                _ => string.Empty
            };
        }

        private static string MapHardwareEncoderKeyToDisplay(string? key)
        {
            return (key ?? string.Empty).ToLower() switch
            {
                "nvenc" => "NVENC",
                "amf" => "AMF",
                "qsv" => "QSV",
                _ => "None"
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
            return BuildConversionOptions(FilePath);
        }

        private ConversionOptions BuildConversionOptions(string inputPath)
        {
            string format = (SelectedFormat ?? "MP4").ToLower();
            string outputDir = string.IsNullOrEmpty(OutputDirectory)
                ? Path.GetDirectoryName(inputPath) ?? Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)
                : OutputDirectory;

            string fileName = !IsBatchMode && !string.IsNullOrEmpty(OutputFileName)
                ? OutputFileName
                : Path.GetFileNameWithoutExtension(inputPath) + "." + format;

            if (!fileName.EndsWith("." + format, StringComparison.OrdinalIgnoreCase))
                fileName += "." + format;

            string outputPath = Path.Combine(outputDir, fileName);

            return new ConversionOptions
            {
                InputPath = inputPath,
                OutputPath = outputPath,
                OutputFormat = format,
                VideoCodec = SelectedVideoCodec ?? "libx264",
                AudioCodec = SelectedAudioCodec ?? "aac",
                HardwareEncoder = MapHardwareEncoderDisplayToKey(SelectedHardwareEncoder ?? string.Empty),
                AudioOnly = IsAudioOnlyMode,
                Crf = !IsAudioOnlyMode && Crf > 0 ? Crf : null,
                VideoBitrate = !IsAudioOnlyMode && VideoBitrate > 0 ? VideoBitrate : null,
                AudioBitrate = AudioBitrate > 0 ? AudioBitrate : null,
                Preset = SelectedPreset ?? "medium"
            };
        }

        [RelayCommand]
        private async Task ConvertAsync()
        {
            if (IsBatchMode)
            {
                await ConvertBatchAsync();
                return;
            }

            if (string.IsNullOrEmpty(FilePath) || !File.Exists(FilePath))
            {
                _messageService.Warning(_loc["SelectFileForConversion"], _loc["ErrorTitle"]);
                return;
            }

            if (!await EnsureFfmpegAvailableAsync())
                return;

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

                var result = await _ffmpegAction.ConvertVideoAsync(options, progress, _cts.Token);

                if (result.Success)
                {
                    _messageService.Info(string.Format(_loc["ConversionSuccess"], result.OutputPath), _loc["SuccessTitle"]);
                    await SaveSettingsAsync();
                }
                else if (result.Cancelled)
                {
                    StatusMessage = _loc["DownloadCancelled"];
                    ProgressPercent = 0;
                }
                else
                {
                    // раньше ошибка ffmpeg молча проглатывалась и конвертация «просто не начиналась»
                    StatusMessage = string.Format(_loc["ConversionError"], result.Error);
                    _messageService.Error(StatusMessage, _loc["ErrorTitle"]);
                    ProgressPercent = 0;
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

        // ==== Пакетная конвертация ====

        /// <summary>Последовательно конвертирует все файлы списка с общим прогрессом.</summary>
        private async Task ConvertBatchAsync()
        {
            var files = BatchFiles.Where(File.Exists).ToList();
            if (files.Count == 0)
            {
                _messageService.Warning(_loc["BatchEmptyWarning"], _loc["WarningTitle"]);
                return;
            }

            var optionsList = files.Select(BuildConversionOptions).ToList();

            // Папка вывода одна на весь пакет
            string? outputDir = Path.GetDirectoryName(optionsList[0].OutputPath);
            if (!string.IsNullOrEmpty(outputDir) && !Directory.Exists(outputDir))
            {
                try { Directory.CreateDirectory(outputDir); }
                catch
                {
                    _messageService.Error(
                        string.Format(_loc["NoSaveFolderAccess"], outputDir), _loc["ErrorTitle"]);
                    return;
                }
            }

            var existing = optionsList.Where(o => File.Exists(o.OutputPath)).ToList();
            if (existing.Count > 0 &&
                !await _dialogService.AskAsync(_loc["BatchOverwriteAsk"], _loc["ConfirmationTitle"]))
            {
                optionsList = optionsList.Where(o => !existing.Contains(o)).ToList();
                if (optionsList.Count == 0)
                    return;
            }

            IsConverting = true;
            IsCancellable = true;
            _cts = new CancellationTokenSource();
            int done = 0;
            int failed = 0;
            bool cancelled = false;
            var failures = new List<string>();

            try
            {
                foreach (var options in optionsList)
                {
                    int index = done + failed + 1;
                    var progress = new Progress<DownloadProgress>(p =>
                    {
                        ProgressPercent = (int)Math.Round((done + failed + p.Percent / 100.0) / optionsList.Count * 100);
                        StatusMessage = $"[{index}/{optionsList.Count}] {Path.GetFileName(options.InputPath)} — {p.Percent}%";
                    });

                    var result = await _ffmpegAction.ConvertVideoAsync(options, progress, _cts.Token);
                    if (result.Cancelled)
                    {
                        cancelled = true;
                        break;
                    }

                    if (!result.Success)
                    {
                        failed++;
                        failures.Add($"{Path.GetFileName(options.InputPath)}: {result.Error}");
                    }

                    done++;
                }

                if (cancelled)
                {
                    StatusMessage = _loc["DownloadCancelled"];
                    ProgressPercent = 0;
                }
                else
                {
                    ProgressPercent = 100;
                    StatusMessage = string.Format(_loc["BatchDone"], done, failed);
                    await SaveSettingsAsync();

                    if (failures.Count > 0)
                    {
                        AppLog.Error("Converter", $"Batch: {done} ok, {failed} failed{Environment.NewLine}{string.Join(Environment.NewLine, failures)}");
                        var details = failures.Take(3);
                        string extra = failures.Count > details.Count()
                            ? Environment.NewLine + "…"
                            : string.Empty;
                        _messageService.Warning(
                            string.Format(_loc["BatchDone"], done, failed)
                                + Environment.NewLine + Environment.NewLine
                                + string.Join(Environment.NewLine, details) + extra,
                            _loc["WarningTitle"]);
                    }
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
        private void AddBatchFiles()
        {
            var dialog = new OpenFileDialog
            {
                Filter = _loc["VideoFileFilter"],
                Title = _loc["SelectVideoFileDialogTitle"],
                Multiselect = true
            };

            if (dialog.ShowDialog() != true)
                return;

            foreach (var file in dialog.FileNames)
            {
                if (!BatchFiles.Contains(file))
                    BatchFiles.Add(file);
            }
            ConvertCommand.NotifyCanExecuteChanged();
        }

        /// <summary>Добавляет файлы в пакет (drag&drop из окна).</summary>
        public void AddBatchFiles(IEnumerable<string> files)
        {
            foreach (var file in files.Where(File.Exists))
            {
                if (!BatchFiles.Contains(file))
                    BatchFiles.Add(file);
            }
            ConvertCommand.NotifyCanExecuteChanged();
        }

        [RelayCommand]
        private void RemoveBatchFile()
        {
            if (SelectedBatchFile != null)
            {
                BatchFiles.Remove(SelectedBatchFile);
                SelectedBatchFile = BatchFiles.LastOrDefault();
                ConvertCommand.NotifyCanExecuteChanged();
            }
        }

        [RelayCommand]
        private void ClearBatchFiles()
        {
            BatchFiles.Clear();
            SelectedBatchFile = null;
            ConvertCommand.NotifyCanExecuteChanged();
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

        /// <summary>
        /// Проверяет доступность ffmpeg для конвертации; при отсутствии предлагает скачать
        /// через встроенный обновлятор и повторяет проверку.
        /// </summary>
        private async Task<bool> EnsureFfmpegAvailableAsync()
        {
            if (FFmpegAction.EnsureExecutablesPath())
                return true;

            if (!await _dialogService.AskAsync(_loc["FfmpegMissingConvert"], _loc["ErrorTitle"]))
                return false;

            IsConverting = true;
            try
            {
                StatusMessage = _loc["CheckingFFmpeg"];
                var info = await _ffmpegService.CheckForUpdateAsync();

                if (string.IsNullOrEmpty(info.DownloadUrl))
                {
                    _messageService.Error(_loc["FFmpegDownloadLinkError"], _loc["UpdateErrorTitle"]);
                    return false;
                }

                var progress = new Progress<DownloadProgress>(p =>
                {
                    StatusMessage = p.StatusMessage;
                    ProgressPercent = p.Percent;
                });

                await _ffmpegService.DownloadUpdateAsync(info, progress);
            }
            catch (Exception ex)
            {
                _messageService.Error(string.Format(_loc["FFmpegUpdateFailed"], ex.Message), _loc["UpdateErrorTitle"]);
                return false;
            }
            finally
            {
                IsConverting = false;
                ProgressPercent = 0;
                StatusMessage = string.Empty;
            }

            FFmpegAction.ResetExecutablesPath();
            if (!FFmpegAction.EnsureExecutablesPath())
            {
                _messageService.Error(_loc["FfmpegMissingConvert"], _loc["ErrorTitle"]);
                return false;
            }

            return true;
        }

        private async Task SaveSettingsAsync()
        {
            var settings = await _settingsService.LoadAsync().ConfigureAwait(true);
            settings.ConvertOutputFormat = SelectedFormat ?? "MP4";
            settings.ConvertAudioOnlyMode = IsAudioOnlyMode;
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
