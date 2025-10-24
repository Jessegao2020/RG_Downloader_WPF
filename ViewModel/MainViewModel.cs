using RedgifsDownloader.Model;
using RedgifsDownloader.Services;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;

namespace RedgifsDownloader.ViewModel
{
    public class MainViewModel : INotifyPropertyChanged
    {
        #region 字段 属性
        private readonly CrawlService _crawler;
        private readonly DownloadCoordinator _coordinator;
        private CancellationTokenSource? _cts;

        public ObservableCollection<VideoItem> Videos { get; } = new();
        public ICollectionView ActiveVideosView { get; }
        public ICollectionView FailedVideosView { get; }

        private bool _isCrawling;
        private bool _isDownloading;
        private int _completedCount;
        private int _failedCount;
        private string _username;
        private int _maxConcurrency;

        public string CrawlBtnText => IsCrawling ? "Crawling.." : "Crawl";

        public int VideosCount => Videos.Count;

        public bool IsCrawling
        {
            get => _isCrawling;
            set
            {
                if (_isCrawling != value)
                {
                    _isCrawling = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(CrawlBtnText));
                }
            }
        }

        public bool IsDownloading
        {
            get => _isDownloading;
            set { _isDownloading = value; OnPropertyChanged(); ((RelayCommand)StopCommand).RaiseCanExecuteChanged(); }
        }

        public string Username
        {
            get => _username;
            set
            {
                _username = value;
                OnPropertyChanged();
                (CrawlCommand as RelayCommand)?.RaiseCanExecuteChanged();
            }
        }

        public int CompletedCount
        {
            get => _completedCount;
            set { _completedCount = value; OnPropertyChanged(); }
        }

        public int FailedCount
        {
            get => _failedCount;
            set
            {
                _failedCount = value; 
                OnPropertyChanged();
                (RetryAllCommand as RelayCommand)?.RaiseCanExecuteChanged();
            }
        }

        public int MaxConcurrency
        {
            get => _maxConcurrency;
            set
            {
                if (_maxConcurrency != value)
                {
                    _maxConcurrency = value; OnPropertyChanged();
                }
            }
        }

        public ICommand CrawlCommand { get; }
        public ICommand DownloadCommand { get; }
        public ICommand StopCommand { get; }
        public ICommand RetryAllCommand { get; }
        #endregion

        public MainViewModel(DownloadCoordinator coordinator, CrawlService crawler)
        {
            _crawler = crawler;
            _coordinator = coordinator;

            ActiveVideosView = CollectionViewSource.GetDefaultView(Videos);
            ActiveVideosView.Filter = o =>
            {
                var v = (VideoItem)o;
                return v.Status is not (VideoStatus.WriteError or VideoStatus.NetworkError or VideoStatus.UnknownError or VideoStatus.Canceled);
            };

            FailedVideosView = new CollectionViewSource { Source = Videos }.View;
            FailedVideosView.Filter = o =>
            {
                var v = (VideoItem)o;
                return v.Status is VideoStatus.WriteError or VideoStatus.NetworkError or VideoStatus.UnknownError or VideoStatus.Canceled;
            };

            // 订阅进度刷新事件
            _coordinator.StatusUpdated += RefreshViews;

            MaxConcurrency = Properties.Settings.Default.MaxDownloadCount;

            CrawlCommand = new RelayCommand(async _ => await StartCrawlAsync(), _ => !IsCrawling && !IsDownloading && !string.IsNullOrWhiteSpace(Username));

            DownloadCommand = new RelayCommand(async _ => await StartDownloadAsync(MaxConcurrency), _ => !IsDownloading && Videos.Any());

            StopCommand = new RelayCommand(_ => CancelDownload(), _ => IsDownloading);

            RetryAllCommand = new RelayCommand(async _ => await RetryAllAsync(), _ => FailedCount > 0);

            Videos.CollectionChanged += (_, __) =>
            {
                OnPropertyChanged(nameof(VideosCount));
                (DownloadCommand as RelayCommand)?.RaiseCanExecuteChanged();
            };
        }

        private static bool IsFailed(VideoItem v)
            => v.Status is VideoStatus.WriteError or VideoStatus.NetworkError or VideoStatus.UnknownError or VideoStatus.Canceled;

        private async Task StartCrawlAsync()
        {
            IsCrawling = true;
            Videos.Clear();
            try
            {
                var list = await _crawler.CrawlAsync(Username, msg => MessageBox.Show(msg, "爬虫错误", MessageBoxButton.OK, MessageBoxImage.Error));

                foreach (var video in list)
                    Videos.Add(video);

                MessageBox.Show($"共爬取到 {Videos.Count} 个视频。");
            }
            catch (Exception ex) { MessageBox.Show($"爬虫异常: {ex.Message}"); }
            finally { IsCrawling = false; }
        }

        public async Task StartDownloadAsync(int concurrency)
        {
            IsDownloading = true;
            _cts = new CancellationTokenSource();

            try
            {
                var summary = await _coordinator.RunDownloadsAsync(Videos, concurrency, _cts.Token);
                CompletedCount = summary.Completed;
                FailedCount = summary.Failed;

                MessageBox.Show($"任务结束。\n成功: {summary.Completed}，失败: {summary.Failed}。");
            }
            catch (OperationCanceledException) { }
            finally
            {
                IsDownloading = false;
                _cts.Dispose();
                _cts = null;
            }
        }

        private async Task RetryAllAsync()
        {
            foreach (var video in Videos.Where(IsFailed))
            {
                video.Status = VideoStatus.Pending;
                video.Progress = 0;
            }

            await StartDownloadAsync(MaxConcurrency);
        }

        private void CancelDownload() => _cts?.Cancel();

        private void RefreshViews()
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                ActiveVideosView.Refresh();
                FailedVideosView.Refresh();

                CompletedCount = Videos.Count(v => v.Status == VideoStatus.Completed);
                FailedCount = Videos.Count(v => IsFailed(v));
            });
        }

        private void CountLiveResult()
        {
            CompletedCount = Videos.Count(v => v.Status == VideoStatus.Completed);
            FailedCount = Videos.Count(v => v.Status is VideoStatus.Canceled or VideoStatus.WriteError or VideoStatus.NetworkError or VideoStatus.UnknownError);
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? name = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
