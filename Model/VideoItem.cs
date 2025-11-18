using RedgifsDownloader.Domain.Enums;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace RedgifsDownloader.Model
{
    public class VideoItem : INotifyPropertyChanged
    {
        private VideoStatus _status;
        private double? _progress;

        public long ExpectedSize { get; set; }
        public long? CreateDateRaw { get; set; }
        public string? Username { get; set; }
        public string? Id { get; set; }
        public string? Url { get; set; }
        public string Token { get; set; } = "";

        // 状态
        public double? Progress
        {
            get => _progress;
            set
            {
                if (_progress != value)
                {
                    _progress = value;
                    OnPropertyChanged(nameof(Progress));
                }
            }
        }
        public VideoStatus Status
        {
            get => _status;
            set
            {
                if (_status != value)
                {
                    _status = value;
                    OnPropertyChanged(nameof(Status));
                }
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
