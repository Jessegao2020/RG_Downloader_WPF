using RedgifsDownloader.Helpers;
using RedgifsDownloader.Interfaces;
using RedgifsDownloader.Services.Reddit;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Printing;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;

namespace RedgifsDownloader.ViewModel
{

    public class RedditViewModel : INotifyPropertyChanged
    {
        private readonly IRedditAuthService _auth;
        private readonly IRedditApiService _api;
        private readonly RedditImageDownloadService _downloader;
        private readonly RedditDownloadCoordinator _coordinator;
        private readonly ILogService _logger;

        private string _usernameInput = "";
        private string _submittedJson = "";
        private bool _isLoggedIn = false;

        public ICommand LoginCommand { get; }
        public ICommand GetSubmittedCommand { get; }

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

        public string SubmittedJson
        {
            get => _submittedJson;
            set { _submittedJson = value; OnPropertyChanged(); }
        }

        public bool IsLoggedIn
        {
            get => _isLoggedIn;
            set
            {
                _isLoggedIn = value;
                OnPropertyChanged();
                (GetSubmittedCommand as RelayCommand)?.RaiseCanExecuteChanged();
            }
        }


        public RedditViewModel(IRedditApiService api, IRedditAuthService auth, RedditDownloadCoordinator coordinator, ILogService logger)
        {
            _auth = auth;
            _api = api;
            _logger = logger;
            _downloader = new RedditImageDownloadService();
            _coordinator = coordinator;

            LoginCommand = new RelayCommand(async _ => await TryLogin());
            GetSubmittedCommand = new RelayCommand(async _ => await GetSubmittedAsync(), _ => IsLoggedIn && !string.IsNullOrEmpty(UsernameInput));
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
        private async Task GetSubmittedAsync()
        {
            try
            {
                string baseDir = Path.Combine(AppContext.BaseDirectory, "Downloads", UsernameInput);
                SubmittedJson = "正在检查并下载中...";

                var (downloaded, skipped) = await _coordinator.DownloadAllImagesAsync(UsernameInput, baseDir, concurrency: 8);
                SubmittedJson = $"下载完成：新下载 {downloaded} 张，跳过 {skipped} 张。";
            }
            catch (Exception ex)
            {
                SubmittedJson = $"Error: {ex.Message}";
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string name = "") =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
