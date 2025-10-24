using RedgifsDownloader.Model;
using RedgifsDownloader.Services;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;

namespace RedgifsDownloader.ViewModel
{
    public class MainViewModel : INotifyPropertyChanged
    {
# region 字段 属性
        private readonly CrawlService _crawler;
        private readonly DownloadCoordinator _coordinator;
        private CancellationTokenSource? _cts;

        public ObservableCollection<VideoItem> Videos { get; } = new();
        public ObservableCollection<VideoItem> Failed { get; } = new();

        private bool _isCrawling;
        private bool _isDownloading;
        private int _completedCount;
        private int _failedCount;
        private string _username;

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
            set { _failedCount = value; OnPropertyChanged(); }
        }

        public ICommand CrawlCommand { get; }
        public ICommand DownloadCommand { get; }
        public ICommand StopCommand { get; }
        #endregion

        public MainViewModel(DownloadCoordinator coordinator, CrawlService crawler)
        {
            _crawler = crawler;
            _coordinator = coordinator;

            CrawlCommand = new RelayCommand(async _ => await StartCrawlAsync(), _ => !IsCrawling && !IsDownloading && !string.IsNullOrWhiteSpace(Username));

            DownloadCommand = new RelayCommand(async _ => await StartDownloadAsync(5), _ => !IsDownloading && Videos.Any());

            StopCommand = new RelayCommand(_ => CancelDownload(), _ => IsDownloading);

            Videos.CollectionChanged += (_, __) =>
            {
                OnPropertyChanged(nameof(VideosCount));
                (DownloadCommand as RelayCommand)?.RaiseCanExecuteChanged();
            };
        }

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
                var summary = await _coordinator.RunDownloadsAsync(Videos, concurrency, _cts.Token, UpdateStatusCounters);
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

        private void CancelDownload()
        {
            _cts?.Cancel();
        }

        private void UpdateStatusCounters()
        {
            CompletedCount = Videos.Count(v => v.Status == VideoStatus.Completed);
            FailedCount = Videos.Count(v => v.Status is VideoStatus.Canceled or VideoStatus.WriteError or VideoStatus.NetworkError or VideoStatus.UnknownError);
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? name = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
