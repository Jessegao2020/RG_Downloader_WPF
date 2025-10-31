using RedgifsDownloader.Model;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace RedgifsDownloader.ViewModel
{
    public class VideoViewModel : INotifyPropertyChanged
    {
        public VideoItem Item { get; }

        public VideoViewModel(VideoItem item)
        {
            Item = item;

            Item.PropertyChanged += (_, e) =>
            {
                OnPropertyChanged(e.PropertyName);
                if (e.PropertyName is nameof(VideoItem.Status) or nameof(VideoItem.Progress))
                    OnPropertyChanged(nameof(DisplayStatus));
            };
        }

        public string Id => Item.Id ?? "";
        public string Username => Item.Username ?? "";
        public string Url => Item.Url ?? "";

        public VideoStatus Status => Item.Status;
        public double? Progress => Item.Progress;
        public long? CreateDateRaw
        {
            get => Item.CreateDateRaw;
            set
            {
                Item.CreateDateRaw = value;
                OnPropertyChanged(nameof(DisplayCreateDate));
            }
        }
        public DateTime? SortCreateDate => Item.CreateDateRaw is long s ? 
            DateTimeOffset.FromUnixTimeSeconds(s).ToLocalTime().DateTime : (DateTime?)null;
        public string DisplayCreateDate
        {
            get
            {
                if (Item.CreateDateRaw is not long raw) return "";
                var date = DateTimeOffset.FromUnixTimeSeconds(raw).ToLocalTime().DateTime;
                return date.ToString("MM/dd/yy");
            }
        }
        public string DisplayStatus =>
        Status switch
        {
            VideoStatus.Pending => "",
            VideoStatus.Downloading when Progress.HasValue => $"{Progress.Value:F1}%",
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

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? name = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
