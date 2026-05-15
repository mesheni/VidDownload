using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace VidDownload.WPF.Control
{
    public class QueueItem : INotifyPropertyChanged
    {
        private string _url;
        private string _title;
        private string _thumbnailUrl;
        private string _status;
        private bool _isProcessing;

        public string Url
        {
            get => _url;
            set { _url = value; OnPropertyChanged(); }
        }

        public string Title
        {
            get => _title;
            set { _title = value; OnPropertyChanged(); }
        }

        public string ThumbnailUrl
        {
            get => _thumbnailUrl;
            set { _thumbnailUrl = value; OnPropertyChanged(); }
        }

        public string Status
        {
            get => _status;
            set { _status = value; OnPropertyChanged(); }
        }

        public bool IsProcessing
        {
            get => _isProcessing;
            set { _isProcessing = value; OnPropertyChanged(); }
        }

        public QueueItem(string url)
        {
            _url = url;
            _title = url;
            _thumbnailUrl = string.Empty;
            _status = "Ожидание";
            _isProcessing = false;
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string? name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
