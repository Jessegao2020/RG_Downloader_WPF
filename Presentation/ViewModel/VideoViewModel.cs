using RedgifsDownloader.Domain.Entities;
using RedgifsDownloader.Domain.Enums;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace RedgifsDownloader.Presentation.ViewModel
{
    public class VideoViewModel : INotifyPropertyChanged
    {
        private bool _isSelected;

        public Action? RefreshFilters { get; set; }
        public Video Item { get; }

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

        public double? Progress => Item.Progress;
        public long? CreateDateRaw => Item.CreateDateRaw;

        public string Id => Item.Id;
        public string Username => Item.Username;
        public string Url => Item.Url.ToString();

        public VideoViewModel(Video item)
        {
            Item = item;
            item.Onchanged += () =>
            {
                OnPropertyChanged(nameof(Status));
                OnPropertyChanged(nameof(Progress));
                OnPropertyChanged(nameof(DisplayStatus));

                RefreshFilters?.Invoke();
            };
        }

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
