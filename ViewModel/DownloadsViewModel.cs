using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using RedgifsDownloader.Helpers;
using RedgifsDownloader.Interfaces;
using RedgifsDownloader.Model;
using RedgifsDownloader.Services.RedGifs;

namespace RedgifsDownloader.ViewModel
{
    public class DownloadsViewModel : INotifyPropertyChanged
    {
        #region 字段 属性
        private readonly ICrawlService _crawler;
        private readonly DownloadCoordinator _coordinator;
        private readonly ISettingsService _settingsService;
        private readonly ILogService _logger;

        public ObservableCollection<VideoViewModel> Videos { get; } = new();
        public ICollectionView ActiveVideosView { get; }
        public ICollectionView FailedVideosView { get; }

        private CancellationTokenSource? _cts;
        private bool _isCrawling;
        private bool _isDownloading;
        private int _completedCount;
        private int _failedCount;
        private string _username;
        private bool _isAllSelected;

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
        public bool IsAllSelected
        {
            get => _isAllSelected;
            set
            {
                if (_isAllSelected == value) return;
                _isAllSelected = value;
                OnPropertyChanged();
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

        private bool IsFailed(VideoViewModel v)
            => v.Status is VideoStatus.WriteError or VideoStatus.NetworkError or VideoStatus.UnknownError or VideoStatus.Canceled;
        public int VideosCount => Videos.Count;
        public string CrawlBtnText => IsCrawling ? "Crawling.." : "Crawl";
        public string DownloadBtnText => IsDownloading ? "下载中" : "下载";

        public ICommand CrawlCommand { get; }
        public ICommand DownloadCommand { get; }
        public ICommand StopCommand { get; }
        public ICommand RetryAllCommand { get; }
        public ICommand SelectAllCommand { get; }
        #endregion

        public DownloadsViewModel(DownloadCoordinator coordinator, ICrawlService crawler, ISettingsService settingsService, ILogService logger)
        {
            _crawler = crawler;
            _coordinator = coordinator;
            _settingsService = settingsService;
            _logger = logger;

            ActiveVideosView = CollectionViewSource.GetDefaultView(Videos);
            ActiveVideosView.Filter = o =>
            {
                var v = (VideoViewModel)o;
                return v.Status is not (VideoStatus.WriteError or VideoStatus.NetworkError or VideoStatus.UnknownError or VideoStatus.Canceled);
            };

            FailedVideosView = new CollectionViewSource { Source = Videos }.View;
            FailedVideosView.Filter = o =>
            {
                var v = (VideoViewModel)o;
                return v.Status is VideoStatus.WriteError or VideoStatus.NetworkError or VideoStatus.UnknownError or VideoStatus.Canceled;
            };

            // 订阅进度刷新事件
            _coordinator.StatusUpdated += RefreshViews;

            #region Commands
            CrawlCommand = new RelayCommand(async _ => await StartCrawlAsync(), _ => !IsCrawling && !IsDownloading && !string.IsNullOrWhiteSpace(Username));
            DownloadCommand = new RelayCommand(async _ => await ExecuteDownloadAsync(), _ => !IsDownloading && Videos.Any());
            StopCommand = new RelayCommand(_ => CancelDownload(), _ => IsDownloading);
            RetryAllCommand = new RelayCommand(async _ => await RetryAllAsync(), _ => FailedCount > 0 && !IsDownloading);
            SelectAllCommand = new RelayCommand(param =>
            {
                bool select = param is bool b && b;
                foreach (var v in Videos)
                    v.IsSelected = select;
                IsAllSelected = select;
            });

            #endregion

            Videos.CollectionChanged += (_, __) =>
            {
                OnPropertyChanged(nameof(VideosCount));
                (DownloadCommand as RelayCommand)?.RaiseCanExecuteChanged();
            };

            // 联动刷新 IsAllSelected
            foreach (var v in Videos)
                v.PropertyChanged += (_, e) =>
                {
                    if (e.PropertyName == nameof(VideoViewModel.IsSelected))
                        UpdateIsAllSelected();
                };
        }

        private async Task StartCrawlAsync()
        {
            IsCrawling = true;
            Videos.Clear();
            try
            {
                await Task.Run(async () =>
                {
                    await foreach (var video in _crawler.CrawlAsync(Username, msg =>
                    {
                        Application.Current.Dispatcher.InvokeAsync(() =>
                            MessageBox.Show(msg, "爬虫错误", MessageBoxButton.OK, MessageBoxImage.Error));
                    }))
                    {
                        // 注意：不能在后台线程直接改 UI
                        await Application.Current.Dispatcher.InvokeAsync(() =>
                        {
                            Videos.Add(new VideoViewModel(video));
                            OnPropertyChanged(nameof(VideosCount));
                        });
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.ShowMessage($"爬虫异常: {ex.Message}");
            }
            finally
            {
                IsCrawling = false;
                _logger.ShowMessage($"任务结束，共爬取{Videos.Count}个视频");
            }
        }

        private async Task ExecuteDownloadAsync()
        {
            var selectedVideos = Videos
        .Where(vm => vm.IsSelected)
        .Select(vm => vm.Item)
        .ToList();

            if (!selectedVideos.Any())
            {
                _logger.ShowMessage("请选择要下载的视频。");
                return;
            }

            await StartDownloadAsync(selectedVideos, _settingsService.MaxDownloadCount, strictCheck: false);
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

                _logger.ShowMessage($"任务结束。\n成功: {summary.Completed}，失败: {summary.Failed}。");
            }
            catch (OperationCanceledException) { }
            finally
            {
                IsDownloading = false;
                _cts.Dispose();
                _cts = null;

                RefreshViews();
                ((RelayCommand)RetryAllCommand).RaiseCanExecuteChanged();
            }
        }

        private async Task RetryAllAsync()
        {
            var failedViewModels = Videos.Where(vm => vm.Status is
                                VideoStatus.Failed
                                or VideoStatus.NetworkError
                                or VideoStatus.UnknownError
                                or VideoStatus.WriteError
                                or VideoStatus.Canceled)
                .ToList();

            var failedVideos = failedViewModels.Select(vm => vm.Item).ToList();

            await StartDownloadAsync(failedVideos, _settingsService.MaxDownloadCount, strictCheck: true);
        }

        private void CancelDownload() => _cts?.Cancel();

        private void RefreshViews()
        {
            Application.Current.Dispatcher.BeginInvoke(() =>
            {
                CompletedCount = Videos.Count(video => video.Status is VideoStatus.Completed or VideoStatus.Exists);
                FailedCount = Videos.Count(v => IsFailed(v));
            });
        }

        private void UpdateIsAllSelected()
        {
            if (Videos.Count == 0) return;
            IsAllSelected = Videos.All(v => v.IsSelected);
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? name = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
