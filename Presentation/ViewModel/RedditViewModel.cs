using RedgifsDownloader.ApplicationLayer.Reddit;
using RedgifsDownloader.ApplicationLayer.Settings;
using RedgifsDownloader.Domain.Interfaces;
using RedgifsDownloader.Presentation.Helpers;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace RedgifsDownloader.Presentation.ViewModel
{
    public class RedditViewModel : INotifyPropertyChanged
    {
        private readonly IRedditAuthService _redditAuth;
        private readonly IRedditDownloadAppService _redditApp;
        private readonly IAppSettings _settings;

        private bool _isLoggedIn;
        private bool _isVideoMode;
        private string _username;
        private string _logContent;
        private int _downloadCount;
        private int _progress;

        public string LoginBtnText => IsLoggedIn ? "Logged In" : "Login";
        public bool IsLoggedIn
        {
            get => _isLoggedIn;
            set
            {
                if (_isLoggedIn == value) return;
                _isLoggedIn = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(LoginBtnText));
                (LoginCommand as RelayCommand)?.RaiseCanExecuteChanged();
                (DownloadCommand as RelayCommand)?.RaiseCanExecuteChanged();
            }
        }
        public bool IsVideoMode
        {
            get => _isVideoMode;
            set
            {
                _isVideoMode = value;
                OnPropertyChanged();
            }
        }
        public string Username
        {
            get => _username;
            set
            {
                _username = value;
                OnPropertyChanged();
                (DownloadCommand as RelayCommand)?.RaiseCanExecuteChanged();
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
        public string LogContent
        {
            get => _logContent;
            set
            {
                _logContent = value;
                OnPropertyChanged();
            }
        }
        public int Progress
        {
            get => _progress;
            set
            {
                _progress = value;
                OnPropertyChanged();
            }
        }
        public ICommand LoadedCommand { get; }
        public ICommand LoginCommand { get; }
        public ICommand DownloadCommand { get; }

        public RedditViewModel(IRedditAuthService redditAuth, IRedditDownloadAppService redditApp, IAppSettings settings)
        {
            _redditAuth = redditAuth;
            _redditApp = redditApp;
            _settings = settings;

            LoginCommand = new RelayCommand(async _ => await Login(), _ => !IsLoggedIn);
            DownloadCommand = new RelayCommand(async _ => await Download(), _ => IsLoggedIn && !string.IsNullOrEmpty(Username));
        }

        private async Task Login()
        {
            bool ok = await _redditApp.LoginAsync();
            LogContent += ok ? "登陆成功.\n" : "登陆失败.\n";
        }

        private async Task Download()
        {
            DownloadCount = 0;

            try
            {
                LogContent += "开始下载...\n";

                var summary = await _redditApp.DownloadUserAsync(Username,
                                                                 IsVideoMode,
                                                                 _settings.MaxConcurrentDownloads,
                                                                 msg => LogContent += msg + "\n",
                                                                 p => DownloadCount = p);

                LogContent += $"任务完成： 下载={summary.Success}，跳过={summary.Skip}，失败={summary.Fail}\n";
            }
            catch (Exception ex) { LogContent += $"异常: {ex.Message}\n"; }
        }

        public async Task CheckLoginStatusAsync()
        {
            try
            {
                var token = await _redditAuth.GetAccessTokenAsync();
                if (!string.IsNullOrEmpty(token))
                {
                    IsLoggedIn = true;
                    LogContent += "[状态]: 自动登录成功.\n";
                }
            }
            catch (Exception ex)
            {
                IsLoggedIn = false;
                LogContent += $"[状态]: 自动登录失败: {ex.Message}\n";
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
