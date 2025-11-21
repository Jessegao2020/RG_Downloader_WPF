using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using RedgifsDownloader.ApplicationLayer.Downloads;
using RedgifsDownloader.ApplicationLayer.Settings;
using RedgifsDownloader.Domain.Entities;
using RedgifsDownloader.Domain.Enums;
using RedgifsDownloader.Domain.Interfaces;
using RedgifsDownloader.Presentation.Helpers;

namespace RedgifsDownloader.Presentation.ViewModel
{
    public class DownloadsViewModel : INotifyPropertyChanged
    {
        private readonly IDownloadAppService _downloadService;
        private readonly ILogService _logger;
        private readonly IAppSettings _settings;

        public ObservableCollection<VideoViewModel> Videos { get; } = new();
        public ICollectionView ActiveVideosView { get; }
        public ICollectionView FailedVideosView { get; }

        public ObservableCollection<string> Platforms { get; } = new() { "Redgifs", "Fikfap" };

        private CancellationTokenSource? _cts;
        private bool _isCrawling;
        private bool _isDownloading;
        private bool _isAllSelected;
        private int _completedCount;
        private int _failedCount;
        private string _username = "";
        private string _selectedPlatform = "Redgifs";
        private string? _sortByProperty;
        private ListSortDirection _sortDirection = ListSortDirection.Descending;

        public string Username
        {
            get => _username;
            set { _username = value; OnPropertyChanged(); (CrawlCommand as RelayCommand)?.RaiseCanExecuteChanged(); }
        }

        public string SelectedPlatform
        {
            get => _selectedPlatform;
            set { _selectedPlatform = value; OnPropertyChanged(); }
        }

        public bool IsCrawling
        {
            get => _isCrawling;
            set { _isCrawling = value; OnPropertyChanged(); OnPropertyChanged(nameof(CrawlBtnText)); }
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

        public int CompletedCount
        {
            get => _completedCount;
            set { _completedCount = value; OnPropertyChanged(); }
        }

        public int FailedCount
        {
            get => _failedCount;
            set { _failedCount = value; OnPropertyChanged(); (RetryAllCommand as RelayCommand)?.RaiseCanExecuteChanged(); }
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

        public string CrawlBtnText => IsCrawling ? "Crawling..." : "Crawl";
        public string DownloadBtnText => IsDownloading ? "下载中..." : "下载";
        public int VideosCount => Videos.Count;

        public ICommand CrawlCommand { get; }
        public ICommand DownloadCommand { get; }
        public ICommand StopCommand { get; }
        public ICommand RetryAllCommand { get; }
        public ICommand SelectAllCommand { get; }
        public ICommand SortByDateCommand { get; }
        public ICommand SortByNameCommand { get; }

        public string SortByDateButtonText => _sortByProperty == "SortCreateDate" 
            ? (_sortDirection == ListSortDirection.Ascending ? "时间 ↑" : "时间 ↓")
            : "时间";

        public string SortByNameButtonText => _sortByProperty == "Id"
            ? (_sortDirection == ListSortDirection.Ascending ? "名称 ↑" : "名称 ↓")
            : "名称";

        public DownloadsViewModel(IDownloadAppService downloadService, ILogService logger, IAppSettings settings)
        {
            _downloadService = downloadService;
            _logger = logger;
            _settings = settings;

            ActiveVideosView = CollectionViewSource.GetDefaultView(Videos);
            ActiveVideosView.Filter = v => { var vm = (VideoViewModel)v; return !vm.Item.IsFailed; };

            FailedVideosView = new CollectionViewSource { Source = Videos }.View;
            FailedVideosView.Filter = v => { var vm = (VideoViewModel)v; return vm.Item.IsFailed; };

            #region Commands
            CrawlCommand = new RelayCommand(async _ => await StartCrawlAsync(), _ => !IsCrawling && !IsDownloading && !string.IsNullOrWhiteSpace(Username));

            DownloadCommand = new RelayCommand(async _ => await StartDownloadAsync(), _ => !IsDownloading && Videos.Any());

            StopCommand = new RelayCommand(_ => _cts?.Cancel(), _ => IsDownloading);

            RetryAllCommand = new RelayCommand(async _ => await RetryAllAsync(), _ => FailedCount > 0 && !IsDownloading);

            SelectAllCommand = new RelayCommand(param =>
            {
                bool on = param is bool b && b;
                foreach (var v in Videos)
                    v.IsSelected = on;

                IsAllSelected = on;
            });

            SortByDateCommand = new RelayCommand(_ =>
            {
                if (_sortByProperty == "SortCreateDate")
                {
                    // 切换升序/降序
                    _sortDirection = _sortDirection == ListSortDirection.Ascending 
                        ? ListSortDirection.Descending 
                        : ListSortDirection.Ascending;
                }
                else
                {
                    _sortByProperty = "SortCreateDate";
                    _sortDirection = ListSortDirection.Descending;
                }
                ApplySort();
                OnPropertyChanged(nameof(SortByDateButtonText));
                OnPropertyChanged(nameof(SortByNameButtonText));
            });

            SortByNameCommand = new RelayCommand(_ =>
            {
                if (_sortByProperty == "Id")
                {
                    // 切换升序/降序
                    _sortDirection = _sortDirection == ListSortDirection.Ascending 
                        ? ListSortDirection.Descending 
                        : ListSortDirection.Ascending;
                }
                else
                {
                    _sortByProperty = "Id";
                    _sortDirection = ListSortDirection.Ascending;
                }
                ApplySort();
                OnPropertyChanged(nameof(SortByDateButtonText));
                OnPropertyChanged(nameof(SortByNameButtonText));
            });
            #endregion

            Videos.CollectionChanged += (_, __) =>
            {
                OnPropertyChanged(nameof(VideosCount));
                (DownloadCommand as RelayCommand)?.RaiseCanExecuteChanged();
            };

            Videos.CollectionChanged += (_, __) =>
            {
                foreach (var vm in Videos)
                {
                    vm.PropertyChanged -= OnVideoPropertyChanged;
                    vm.PropertyChanged += OnVideoPropertyChanged;
                }
            };
        }

        private async Task StartCrawlAsync()
        {
            Videos.Clear();
            IsCrawling = true;

            try
            {
                var platformEnum = SelectedPlatform == "Redgifs" ? MediaPlatform.Redgifs : MediaPlatform.Fikfap;
                await Task.Run(async () =>
                {
                    await foreach (Video video in _downloadService.CrawlAsync(
                    platformEnum,
                    Username,
                    msg => Application.Current.Dispatcher.Invoke(() => _logger.ShowMessage(msg)),
                    CancellationToken.None))
                    {
                        await Application.Current.Dispatcher.InvokeAsync(() =>
                        {
                            var vm = new VideoViewModel(video)
                            {
                                RefreshFilters = () =>
                                {
                                    ActiveVideosView.Refresh();
                                    FailedVideosView.Refresh();
                                }
                            };

                            Videos.Add(vm);
                            OnPropertyChanged(nameof(VideosCount));
                        });
                    }
                });
            }
            finally { IsCrawling = false; _logger.ShowMessage($"共爬取{Videos.Count}个视频"); }
        }

        private async Task StartDownloadAsync()
        {
            var selected = ActiveVideosView.Cast<VideoViewModel>().Where(v => v.IsSelected).Select(v => v.Item).ToList();
            if (!selected.Any())
            {
                _logger.ShowMessage("请选择要下载的视频");
                return;
            }

            IsDownloading = true;
            _cts = new CancellationTokenSource();

            try
            {
                var summary = await _downloadService.DownloadAsync(selected, _settings.MaxConcurrentDownloads, _cts.Token);
                _logger.ShowMessage($"下载结束：成功{summary.Completed}, 失败{summary.Failed}");
            }
            finally { IsDownloading = false; _cts = null; }
        }

        private async Task RetryAllAsync()
        {
            var failed = Videos.Where(v => IsFailed(v)).Select(v => v.Item).ToList();

            if (!failed.Any()) return;

            await StartDownloadRetryAsync(failed);
        }

        private async Task StartDownloadRetryAsync(List<Video> videos)
        {
            IsDownloading = true;
            _cts = new CancellationTokenSource();

            try
            {
                var summary = await _downloadService.RetryFailedAsync(videos, _settings.MaxConcurrentDownloads, _cts.Token);
                _logger.ShowMessage($"下载结束：成功{summary.Completed}, 失败{summary.Failed}");
            }
            finally { IsDownloading = false; _cts = null; }
        }

        private bool IsFailed(VideoViewModel v) =>
            v.Status is
            VideoStatus.Failed or
            VideoStatus.NetworkError or
            VideoStatus.WriteError or
            VideoStatus.UnknownError or
            VideoStatus.Canceled;

        private void OnVideoPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(VideoViewModel.Status))
                RefreshCounts();
        }

        private void RefreshCounts()
        {
            CompletedCount = Videos.Count(v => v.Status == VideoStatus.Completed);
            FailedCount = Videos.Count(v => IsFailed(v));
        }

        private void ApplySort()
        {
            if (string.IsNullOrEmpty(_sortByProperty))
                return;

            ActiveVideosView.SortDescriptions.Clear();
            ActiveVideosView.SortDescriptions.Add(new SortDescription(_sortByProperty, _sortDirection));
            ActiveVideosView.Refresh();
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
