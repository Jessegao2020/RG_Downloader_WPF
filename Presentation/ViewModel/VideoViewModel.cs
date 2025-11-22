using RedgifsDownloader.ApplicationLayer.Notifications;
using RedgifsDownloader.Domain.Entities;
using RedgifsDownloader.Domain.Enums;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;

namespace RedgifsDownloader.Presentation.ViewModel
{
    public class VideoViewModel : INotifyPropertyChanged
    {
        private bool _isSelected;
        private bool _lastIsFailed;

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
        public string? ThumbnailUrl => Item.ThumbnailUrl;

        public VideoViewModel(Video item, VideoChangeNotifier notifier)
        {
            Item = item;
            _lastIsFailed = item.IsFailed;
            
            // 订阅来自 Application 层的变化通知
            notifier.Subscribe(HandleVideoChanged);
        }
        
        private void HandleVideoChanged(Video video)
        {
            // 只处理当前 ViewModel 对应的 Video
            if (video != Item) return;
            
            // 必须在 UI 线程更新
            Application.Current.Dispatcher.Invoke(() =>
            {
                OnPropertyChanged(nameof(Status));
                OnPropertyChanged(nameof(Progress));
                OnPropertyChanged(nameof(DisplayStatus));

                // 只在IsFailed状态改变时才刷新Filter，避免频繁刷新导致图片消失
                bool currentIsFailed = Item.IsFailed;
                if (_lastIsFailed != currentIsFailed)
                {
                    _lastIsFailed = currentIsFailed;
                    RefreshFilters?.Invoke();
                }
            });
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
            VideoStatus.Canceled => "已取消",
            VideoStatus.NetworkError => "网络错误",
            VideoStatus.WriteError => "写入错误",
            VideoStatus.UnknownError => "未知错误",
            _ => ""
        };

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? name = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
