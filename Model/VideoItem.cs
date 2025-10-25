using System.ComponentModel;
namespace RedgifsDownloader.Model
{
    public class VideoItem : INotifyPropertyChanged
    {
        public string? Id { get; set; }
        public string? Url { get; set; }
        public string? Username { get; set; }
        public string Token { get; set; } = "";
        public long ExpectedSize { get; set; }

        private double? _progress;
        private VideoStatus _status;

        public VideoStatus Status
        {
            get => _status;
            set
            {
                if (_status != value)
                {
                    _status = value;
                    OnPropertyChanged(nameof(Status));
                    OnPropertyChanged(nameof(DisplayStatus));
                }
            }
        }

        public double? Progress
        {
            get => _progress;
            set
            {
                if (_progress != value)
                {
                    _progress = value;
                    OnPropertyChanged(nameof(Progress));
                    OnPropertyChanged(nameof(DisplayStatus));
                }
            }
        }

        // 👇 这个属性是给 UI 用的
        public string DisplayStatus
        {
            get
            {
                if (Status == VideoStatus.Downloading && Progress.HasValue)
                    return $"{Progress.Value:F1}%";

                return Status switch
                {
                    VideoStatus.Pending => "",
                    VideoStatus.Downloading => "下载中",
                    VideoStatus.Completed => "完成",
                    VideoStatus.Exists => "已存在",
                    VideoStatus.Failed => "失败",
                    VideoStatus.Canceled => "已停止",
                    VideoStatus.NetworkError => "网络错误",
                    VideoStatus.WriteError => "写入错误",
                    VideoStatus.UnknownError => "未知错误",
                    _ => ""
                };
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string name)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
