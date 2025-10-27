using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using RedgifsDownloader.Helpers;
using RedgifsDownloader.Interfaces;
using RedgifsDownloader.Model;
using RedgifsDownloader.Services;

namespace RedgifsDownloader.ViewModel
{
    public class DownloadsViewModel : INotifyPropertyChanged
    {
        #region 字段 属性
        private readonly ICrawlService _crawler;
        private readonly DownloadCoordinator _coordinator;
        private readonly ISettingsService _settingsService;
        private CancellationTokenSource? _cts;

        public ObservableCollection<VideoItem> Videos { get; } = new();
        public ICollectionView ActiveVideosView { get; }
        public ICollectionView FailedVideosView { get; }

        private bool _isCrawling;
        private bool _isDownloading;
        private int _completedCount;
        private int _failedCount;
        private string _username;

        public string CrawlBtnText => IsCrawling ? "Crawling.." : "Crawl";
        public string DownloadBtnText => IsDownloading ? "下载中" : "下载";
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
            set
            {
                _isDownloading = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(DownloadBtnText));
                ((RelayCommand)StopCommand).RaiseCanExecuteChanged();
                ((RelayCommand)DownloadCommand).RaiseCanExecuteChanged();
                ((RelayCommand)RetryAllCommand).RaiseCanExecuteChanged();
            }
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
        public ObservableCollection<int> MaxConcurrencyOptions { get; } = new(new[] {1,2,3,4,5,6,7,8,9,10});
        public int MaxConcurrency
        {
            get => _settingsService.MaxDownloadCount;
            set
            {
                if (_settingsService.MaxDownloadCount != value)
                {
                    _settingsService.MaxDownloadCount = value;
                    _settingsService.Save();
                    OnPropertyChanged();
                }
            }
        }

        public ICommand CrawlCommand { get; }
        public ICommand DownloadCommand { get; }
        public ICommand StopCommand { get; }
        public ICommand RetryAllCommand { get; }

        #endregion

        public DownloadsViewModel(DownloadCoordinator coordinator, ICrawlService crawler, ISettingsService settingsService)
        {
            _crawler = crawler;
            _coordinator = coordinator;
            _settingsService = settingsService;

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
            #region Commands
            CrawlCommand = new RelayCommand(async _ => await StartCrawlAsync(), _ => !IsCrawling && !IsDownloading && !string.IsNullOrWhiteSpace(Username));

            DownloadCommand = new RelayCommand(async _ => await ExecuteDownloadAsync(), _ => !IsDownloading && Videos.Any());

            StopCommand = new RelayCommand(_ => CancelDownload(), _ => IsDownloading);

            RetryAllCommand = new RelayCommand(async _ => await RetryAllAsync(), _ => FailedCount > 0 && !IsDownloading);

            #endregion
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

                if (list.Any())
                {
                    foreach (var video in list)
                        Videos.Add(video);

                    MessageBox.Show($"共爬取到 {Videos.Count} 个视频。");
                }
            }
            catch (Exception ex) { MessageBox.Show($"爬虫异常: {ex.Message}"); }
            finally { IsCrawling = false; }
        }

        private async Task ExecuteDownloadAsync()
        {
            var pendingVideos = Videos.Where(video => video.Status is not
                                            (VideoStatus.NetworkError
                                            or VideoStatus.WriteError
                                            or VideoStatus.UnknownError
                                            or VideoStatus.Canceled))
                .ToList();

            if (!pendingVideos.Any())
            {
                MessageBox.Show("没有可下载视频。");
                return;
            }

            await StartDownloadAsync(pendingVideos, MaxConcurrency, strictCheck: false);
        }

        public async Task StartDownloadAsync(IEnumerable<VideoItem> videos, int concurrency, bool strictCheck = false)
        {
            IsDownloading = true;
            _cts = new CancellationTokenSource();

            try
            {
                var summary = await _coordinator.RunDownloadsAsync(videos, concurrency, strictCheck, _cts.Token);
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
            var failedVideos = Videos.Where(video => video.Status is
                                VideoStatus.Failed
                                or VideoStatus.NetworkError
                                or VideoStatus.UnknownError
                                or VideoStatus.WriteError
                                or VideoStatus.Canceled)
                .ToList();

            await StartDownloadAsync(failedVideos, MaxConcurrency, strictCheck: true);
        }

        private void CancelDownload() => _cts?.Cancel();

        private void RefreshViews()
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                ActiveVideosView.Refresh();
                FailedVideosView.Refresh();

                CompletedCount = Videos.Count(video => video.Status is VideoStatus.Completed or VideoStatus.Exists);
                FailedCount = Videos.Count(v => IsFailed(v));
            });
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? name = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
