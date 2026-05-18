using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using VidDownload.WPF.ViewModels.Base;

namespace VidDownload.WPF.ViewModels
{
    public partial class MainViewModel : ViewModelBase
    {
        [ObservableProperty]
        private string _url = string.Empty;

        [ObservableProperty]
        private string _selectedResolution = string.Empty;

        [ObservableProperty]
        private string _selectedCodec = string.Empty;

        [ObservableProperty]
        private string _selectedAudioFormat = string.Empty;

        [ObservableProperty]
        private string _selectedFormat = string.Empty;

        [ObservableProperty]
        private bool _isPlaylist;

        [ObservableProperty]
        private bool _isAudioOnly;

        [ObservableProperty]
        private bool _isReEncode;

        [ObservableProperty]
        private string _statusMessage = string.Empty;

        [ObservableProperty]
        private int _progressPercent;

        [ObservableProperty]
        private bool _isDownloading;

        [ObservableProperty]
        private bool _isVideoOptionsVisible = true;

        [ObservableProperty]
        private bool _isAudioOptionsVisible;

        [ObservableProperty]
        private string _linkLabelText = "Поле для ссылки на видео:";

        public ObservableCollection<string> Resolutions { get; } = new()
        {
            "", "144", "240", "360", "480", "720", "1080", "1440", "2160"
        };

        public ObservableCollection<string> Codecs { get; } = new()
        {
            "", "av01", "vp9.2", "vp9", "h265", "h264", "vp8", "h263"
        };

        public ObservableCollection<string> AudioFormats { get; } = new()
        {
            "", "mp3", "3gp", "flac", "wav", "aac", "m4a"
        };

        public ObservableCollection<string> Formats { get; } = new()
        {
            "", "avi", "mkv", "mp4", "webm"
        };

        partial void OnIsAudioOnlyChanged(bool value)
        {
            IsVideoOptionsVisible = !value;
            IsAudioOptionsVisible = value;
        }

        partial void OnIsPlaylistChanged(bool value)
        {
            LinkLabelText = value ? "Поле для ссылки на плейлист:" : "Поле для ссылки на видео:";
        }
    }
}
