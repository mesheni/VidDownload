using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using VidDownload.WPF.Control;
using VidDownload.WPF.Resources;
using VidDownload.WPF.Services;

namespace VidDownload.WPF.ViewModels
{
    /// <summary>Элемент плейлиста с галочкой выбора.</summary>
    public partial class PlaylistEntryViewModel : ObservableObject
    {
        public PlaylistEntryInfo Entry { get; }

        [ObservableProperty]
        private bool _isSelected = true;

        public string IndexAndTitle => $"{Entry.Index}. {Entry.Title}";

        public string DurationText => Entry.DurationText;

        public PlaylistEntryViewModel(PlaylistEntryInfo entry)
        {
            Entry = entry;
        }
    }

    /// <summary>Строка формата для ручного выбора в предпросмотре.</summary>
    public partial class FormatOptionViewModel : ObservableObject
    {
        public FormatInfo Format { get; }

        public string DisplayText => Format.DisplayText;

        public FormatOptionViewModel(FormatInfo format)
        {
            Format = format;
        }
    }

    /// <summary>
    /// Предпросмотр метаданных перед загрузкой: обложка, название, длительность,
    /// выбор конкретного формата (для одиночного видео) или элементов (для плейлиста).
    /// </summary>
    public partial class VideoInfoViewModel : ObservableObject
    {
        private readonly LocalizedStrings _loc;

        public LocalizedStrings LocalizedStrings => _loc;

        [ObservableProperty]
        private string _title = string.Empty;

        [ObservableProperty]
        private string _uploader = string.Empty;

        [ObservableProperty]
        private string _durationText = string.Empty;

        [ObservableProperty]
        private string _webpageUrl = string.Empty;

        [ObservableProperty]
        private BitmapImage? _thumbnail;

        [ObservableProperty]
        private bool _isPlaylist;

        /// <summary>«Авто» — не выбирать конкретный формат, использовать сортировку -S.</summary>
        [ObservableProperty]
        private bool _useAutoFormat = true;

        [ObservableProperty]
        private FormatOptionViewModel? _selectedFormat;

        [ObservableProperty]
        private string _selectionSummary = string.Empty;

        public ObservableCollection<PlaylistEntryViewModel> Entries { get; } = new();

        public ObservableCollection<FormatOptionViewModel> Formats { get; } = new();

        public bool HasFormats => Formats.Count > 0;

        /// <summary>Пользователь нажал «Скачать».</summary>
        public bool Confirmed { get; private set; }

        /// <summary>Селектор -f (пустой = авто). Заполняется при подтверждении.</summary>
        public string FormatSelector { get; private set; } = string.Empty;

        /// <summary>Список --playlist-items (пустой = весь плейлист). Заполняется при подтверждении.</summary>
        public string PlaylistItems { get; private set; } = string.Empty;

        public VideoInfoViewModel(LocalizedStrings localizedStrings)
        {
            _loc = localizedStrings;
        }

        public void Initialize(VideoInfo info, bool isAudioOnly)
        {
            Title = info.Title;
            Uploader = info.Uploader;
            DurationText = VideoInfoFormatting.Duration(info.Duration);
            WebpageUrl = info.WebpageUrl;
            IsPlaylist = info.IsPlaylistResult;

            Entries.Clear();
            foreach (var entry in info.Entries)
                Entries.Add(new PlaylistEntryViewModel(entry));
            foreach (var entry in Entries)
                entry.PropertyChanged += (_, _) => RefreshSelectionSummary();

            Formats.Clear();
            if (!isAudioOnly && !info.IsPlaylistResult)
            {
                foreach (var format in MetadataParser.GetSelectableVideoFormats(info))
                    Formats.Add(new FormatOptionViewModel(format));
                SelectedFormat = Formats.FirstOrDefault();
            }
            OnPropertyChanged(nameof(HasFormats));

            RefreshSelectionSummary();
            _ = LoadThumbnailAsync(info.Thumbnail);
        }

        [RelayCommand]
        private void SelectAll()
        {
            foreach (var entry in Entries)
                entry.IsSelected = true;
        }

        [RelayCommand]
        private void SelectNone()
        {
            foreach (var entry in Entries)
                entry.IsSelected = false;
        }

        /// <summary>Кнопка «Скачать»: валидирует выбор и фиксирует результат.</summary>
        [RelayCommand]
        private void Confirm()
        {
            if (IsPlaylist && Entries.All(e => !e.IsSelected))
                return;

            if (IsPlaylist)
                PlaylistItems = BuildPlaylistItems(Entries.Where(e => e.IsSelected).Select(e => e.Entry.Index));

            if (!IsPlaylist && !UseAutoFormat && SelectedFormat != null)
            {
                // Видео без ауда дополняется лучшим аудио-потоком
                FormatSelector = SelectedFormat.Format.IsVideoOnly
                    ? $"{SelectedFormat.Format.FormatId}+ba"
                    : SelectedFormat.Format.FormatId;
            }

            Confirmed = true;
        }

        partial void OnUseAutoFormatChanged(bool value) => RefreshSelectionSummary();

        partial void OnSelectedFormatChanged(FormatOptionViewModel? value) => RefreshSelectionSummary();

        private void RefreshSelectionSummary()
        {
            if (IsPlaylist)
            {
                int selected = Entries.Count(e => e.IsSelected);
                SelectionSummary = string.Format(_loc["PreviewSelectedCount"], selected, Entries.Count);
            }
            else if (!UseAutoFormat && SelectedFormat != null)
            {
                SelectionSummary = SelectedFormat.DisplayText;
            }
            else
            {
                SelectionSummary = _loc["PreviewAutoFormat"];
            }
        }

        /// <summary>Сжимает выбранные индексы в «1-3,7,10-12».</summary>
        internal static string BuildPlaylistItems(IEnumerable<int> indices)
        {
            var sorted = indices.Distinct().OrderBy(i => i).ToList();
            var sb = new StringBuilder();
            int i = 0;
            while (i < sorted.Count)
            {
                int start = sorted[i];
                int end = start;
                while (i + 1 < sorted.Count && sorted[i + 1] == end + 1)
                {
                    end = sorted[++i];
                }

                if (sb.Length > 0)
                    sb.Append(',');
                sb.Append(start == end ? start.ToString() : $"{start}-{end}");
                i++;
            }
            return sb.ToString();
        }

        private async Task LoadThumbnailAsync(string url)
        {
            if (string.IsNullOrEmpty(url))
                return;

            try
            {
                byte[] bytes = await NetworkHelper.Http.GetByteArrayAsync(url).ConfigureAwait(true);
                var image = new BitmapImage();
                using (var ms = new MemoryStream(bytes))
                {
                    image.BeginInit();
                    image.CacheOption = BitmapCacheOption.OnLoad;
                    image.StreamSource = ms;
                    image.EndInit();
                }
                image.Freeze();
                Thumbnail = image;
            }
            catch
            {
                // Обложка не критична — молча оставляем заглушку
            }
        }
    }
}
