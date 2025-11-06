using RedgifsDownloader.Helpers;
using RedgifsDownloader.Interfaces;
using RedgifsDownloader.Model;
using RedgifsDownloader.Services;
using RedgifsDownloader.Services.Reddit;
using RedgifsDownloader.Services.RedGifs;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.Tracing;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;

namespace RedgifsDownloader.ViewModel
{
    public class RedditViewModel : INotifyPropertyChanged
    {
        private readonly IRedditAuthService _auth;
        private readonly IRedditApiService _api;
        private readonly RedditImageDownloadService _downloader;
        private readonly RedditDownloadCoordinator _coordinator;
        private readonly DownloadCoordinator _videoCoordinator;
        private readonly RedgifsAuthService _redgifsAuth;
        private readonly ILogService _logger;

        private bool _isLoggedIn = false;
        private bool _isVideoMode = false;
        private int _downloadCount = 0;
        private string _usernameInput = "";
        private string _submittedJson = "";

        public bool IsLoggedIn
        {
            get => _isLoggedIn;
            set
            {
                _isLoggedIn = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(LoginBtnText));
                (LoginCommand as RelayCommand)?.RaiseCanExecuteChanged();
                (GetSubmittedCommand as RelayCommand)?.RaiseCanExecuteChanged();
            }
        }
        public bool IsVideoMode
        {
            get { return _isVideoMode; }
            set { _isVideoMode = value; OnPropertyChanged(); }
        }
        public string LoginBtnText => IsLoggedIn ? "Logged In" : "Login";
        public string UsernameInput
        {
            get => _usernameInput;
            set
            {
                _usernameInput = value;
                OnPropertyChanged();
                (GetSubmittedCommand as RelayCommand)?.RaiseCanExecuteChanged();
            }
        }
        public int DownloadCount
        {
            get => _downloadCount;
            set
            {
                if (_downloadCount != value)
                {
                    _downloadCount = value;
                    OnPropertyChanged();
                }
            }
        }
        public string SubmittedJson
        {
            get => _submittedJson;
            set { _submittedJson = value; OnPropertyChanged(); }
        }

        public ICommand LoginCommand { get; }
        public ICommand GetSubmittedCommand { get; }

        public RedditViewModel(IRedditApiService api,
                               IRedditAuthService auth,
                               RedditDownloadCoordinator coordinator,
                               RedditImageDownloadService downloader,
                               RedgifsAuthService redgifsAuth,
                               DownloadCoordinator videoCoordinator,
                               ILogService logger)
        {
            _auth = auth;
            _api = api;
            _logger = logger;
            _downloader = downloader;
            _coordinator = coordinator;
            _redgifsAuth = redgifsAuth;
            _videoCoordinator = videoCoordinator;

            LoginCommand = new RelayCommand(async _ => await TryLogin(), _ => !IsLoggedIn);
            GetSubmittedCommand = new RelayCommand(async _ => await StartDownloadAsync(), _ => IsLoggedIn && !string.IsNullOrEmpty(UsernameInput));
            _ = CheckLoginStatus();
        }

        private async Task TryLogin()
        {
            try
            {
                await _auth.LoginAsync();
                IsLoggedIn = true;
            }
            catch (Exception ex)
            {
                SubmittedJson = $"Login failed: {ex.Message}";
            }
        }

        private async Task CheckLoginStatus()
        {
            try
            {
                string token = await _auth.GetAccessTokenAsync();
                if (!string.IsNullOrEmpty(token))
                {
                    IsLoggedIn = true;
                }
            }
            catch (Exception ex)
            {
                _logger.ShowMessage($"[RedditVM] 自动登陆失败:{ex.Message}");
                IsLoggedIn = false;
            }
        }
        private async Task StartDownloadAsync()
        {
            try
            {
                string baseDir = Path.Combine(AppContext.BaseDirectory, "Downloads");
                SubmittedJson = "正在检查并下载中...";
                DownloadCount = 0;

                if (!IsVideoMode)
                {
                    var (downloaded, skipped) = await _coordinator.DownloadAllImagesAsync(UsernameInput,
                                                                                      baseDir,
                                                                                      concurrency: 8,
                                                                                      msg => SubmittedJson += msg + "\n",
                                                                                      count => Application.Current.Dispatcher.Invoke(() => DownloadCount = count));
                    SubmittedJson += $"\n下载完成：新下载 {downloaded} 张，跳过 {skipped} 张。";
                }
                else
                {
                    string token = await _redgifsAuth.GetTokenAsync();
                    Debug.WriteLine(token); 
                    

                    var videoItems = new List<VideoItem>();
                    await foreach (var video in _api.StreamUserRedgifsPostsAsync(UsernameInput))
                    {
                        videoItems.Add(new VideoItem
                        {
                            Url = video.Url,
                            Username = UsernameInput,
                            Token = token // ✅ 带上授权
                        });
                        Debug.WriteLine(video.Url);
                    }
                    Debug.WriteLine($"[VM] Parsed {videoItems.Count} videos to download.");
                    var summary = await _videoCoordinator.RunDownloadsAsync(
                        videoItems, concurrency: 4, strictCheck: false, CancellationToken.None);

                    SubmittedJson += $"\n视频下载完成：成功 {summary.Completed}，失败 {summary.Failed}";
                }
            }
            catch (Exception ex)
            {
                SubmittedJson += $"Error: {ex.Message}";
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string name = "") =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
