using RedgifsDownloader.Domain.Entities;
using RedgifsDownloader.Domain.Enums;
using RedgifsDownloader.Model;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace RedgifsDownloader.Presentation.ViewModel
{
    public class VideoViewModel : INotifyPropertyChanged
    {
        public Video Item { get; }

        public VideoViewModel(Video item)
        {
            Item = item;
        }

        private bool _isSelected;
        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                if (_isSelected != value)
                {
                    _isSelected = value;
                    OnPropertyChanged();
                }
            }
        }

        private double? _progress;
        public double? Progress
        {
            get => _progress;
            set { _progress = value; OnPropertyChanged(); OnPropertyChanged(nameof(DisplayStatus)); }
        }
        public long? CreateDateRaw => Item.CreateDateRaw;

        public string Id => Item.Id;
        public string Username => Item.Username;
        public string Url => Item.Url.ToString();

        public VideoStatus Status => Item.Status;

        public string DisplayCreateDate => SortCreateDate?.ToString("MM/dd/yy") ?? "";

        public DateTime? SortCreateDate =>
            Item.CreateDateRaw is long ts ? DateTimeOffset.FromUnixTimeSeconds(ts).LocalDateTime : null;

        public string DisplayStatus =>
        Status switch
        {
            VideoStatus.Pending => "",
            VideoStatus.Downloading => Progress.HasValue ? $"{Progress.Value:F1}%" : "下载中",
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
