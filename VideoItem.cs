using System.ComponentModel;

namespace RedgifsDownloader
{
    public class VideoItem : INotifyPropertyChanged
    {
        public string? id { get; set; }
        public string? url { get; set; }
        public string token { get; set; } = "";
        public string? userName { get; set; }

        public double? _progress { get; set; }
        public string? _status { get; set; } // 待下载/下载中/完成/失败
        public string status
        {
            get => _status;
            set
            {
                _status = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(status)));
            }
        }
        public double? progress
        {
            get => _progress;
            set
            {
                if (_progress != value)
                {
                    _progress = value;
                    OnPropertyChanged(nameof(progress));
                }
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;        
        protected void OnPropertyChanged(string propertyName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
