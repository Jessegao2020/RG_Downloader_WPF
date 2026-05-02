using System.Collections.Concurrent;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text;
using System.Windows.Input;
using System.Windows.Threading;
using RedgifsDownloader.ApplicationLayer.Reddit;
using RedgifsDownloader.ApplicationLayer.Settings;
using RedgifsDownloader.Domain.Interfaces;
using RedgifsDownloader.Presentation.Helpers;

namespace RedgifsDownloader.Presentation.ViewModel
{
    public class RedditViewModel : INotifyPropertyChanged
    {
        private readonly IAppSettings _settings;
        private readonly IRedditAuthService _redditAuth;
        private readonly IRedditDownloadAppService _redditApp;

        private CancellationTokenSource? _cts;
        private bool _isLoggedIn;
        private bool _isVideoMode;
        private bool _isDownloading;
        private bool _useCutoffDate;

        private string _username = string.Empty;
        private string _logContent = string.Empty;
        private int _downloadCount;
        private int _progress;
        private DateTime? _cutoffDate = DateTime.Today;

        // ---- 日志节流：队列 + StringBuilder + UI 定时刷新 ----
        private readonly ConcurrentQueue<string> _logQueue = new();
        private readonly StringBuilder _logSb = new();
        private readonly DispatcherTimer _uiTick;

        // 进度节流：先存最新值，再在 UI Tick 里刷新到属性
        private int _latestDownloadedCount;

        // 可按你 UI 体验调整
        private const int UiTickMs = 100;           // UI 每 100ms 刷一次
        private const int MaxLinesPerTick = 200;    // 每次最多处理多少行日志
        private const int MaxLogChars = 200_000;    // 最大日志字符数（避免无限增长拖垮 TextBox）

        public string LoginBtnText => IsLoggedIn ? "Logged In" : "Login";
        public string DownloadBtnText => IsDownloading ? "Stop" : "Download";

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
                if (_isVideoMode == value) return;
                _isVideoMode = value;
                OnPropertyChanged();
            }
        }

        public bool IsDownloading
        {
            get => _isDownloading;
            set
            {
                if (_isDownloading == value) return;
                _isDownloading = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(DownloadBtnText));
                (DownloadCommand as RelayCommand)?.RaiseCanExecuteChanged();
            }
        }

        public bool UseCutoffDate
        {
            get => _useCutoffDate;
            set
            {
                if (_useCutoffDate == value) return;
                _useCutoffDate = value;
                OnPropertyChanged();
            }
        }

        public DateTime? CutoffDate
        {
            get => _cutoffDate;
            set
            {
                if (_cutoffDate == value) return;
                _cutoffDate = value;
                OnPropertyChanged();
            }
        }

        public string Username
        {
            get => _username;
            set
            {
                if (_username == value) return;
                _username = value;
                OnPropertyChanged();
                (DownloadCommand as RelayCommand)?.RaiseCanExecuteChanged();
            }
        }

        public int DownloadCount
        {
            get => _downloadCount;
            private set
            {
                if (_downloadCount == value) return;
                _downloadCount = value;
                OnPropertyChanged();
            }
        }

        public string LogContent
        {
            get => _logContent;
            private set
            {
                if (_logContent == value) return;
                _logContent = value;
                OnPropertyChanged();
            }
        }

        public int Progress
        {
            get => _progress;
            set
            {
                if (_progress == value) return;
                _progress = value;
                OnPropertyChanged();
            }
        }

        public ICommand LoadedCommand { get; } // 你原来就有，保留
        public ICommand LoginCommand { get; }
        public ICommand DownloadCommand { get; }

        public RedditViewModel(IRedditAuthService redditAuth, IRedditDownloadAppService redditApp, IAppSettings settings)
        {
            _redditAuth = redditAuth;
            _redditApp = redditApp;
            _settings = settings;

            LoginCommand = new RelayCommand(async _ => await Login(), _ => !IsLoggedIn);
            DownloadCommand = new RelayCommand(async _ => await ToggleDownload(), _ => IsLoggedIn && !string.IsNullOrWhiteSpace(Username));

            // UI Tick：统一刷新日志 + 进度，避免每条日志都触发 TextBox 重绘
            _uiTick = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(UiTickMs) };
            _uiTick.Tick += (_, __) => UiFlush();
            _uiTick.Start();

            _ = CheckLoginStatusAsync();
        }

        private void EnqueueLog(string msg)
        {
            if (string.IsNullOrWhiteSpace(msg)) return;
            _logQueue.Enqueue(msg);
        }

        private void UiFlush()
        {
            // 1) 刷新 DownloadCount（节流）
            var latest = Volatile.Read(ref _latestDownloadedCount);
            if (latest != DownloadCount)
                DownloadCount = latest;

            // 2) 批量刷日志
            int processed = 0;
            while (processed < MaxLinesPerTick && _logQueue.TryDequeue(out var line))
            {
                _logSb.AppendLine(line);
                processed++;
            }

            if (processed == 0) return;

            // 限制日志长度（避免越来越卡）
            if (_logSb.Length > MaxLogChars)
                _logSb.Remove(0, _logSb.Length - MaxLogChars);

            LogContent = _logSb.ToString();
        }

        private async Task Login()
        {
            bool ok = await _redditApp.LoginAsync();
            EnqueueLog(ok ? "登陆成功." : "登陆失败.");
        }

        private async Task ToggleDownload()
        {
            if (IsDownloading)
            {
                EnqueueLog("正在停止...");
                _cts?.Cancel();
                return;
            }

            IsDownloading = true;
            Volatile.Write(ref _latestDownloadedCount, 0);
            DownloadCount = 0;

            _cts = new CancellationTokenSource();

            try
            {
                EnqueueLog("开始下载...");

                // Progress<T> 在 UI 线程创建：Report 会自动 marshal 回 UI 线程
                var logProgress = new Progress<string>(EnqueueLog);

                // 这里不要每次都直接改 DownloadCount（会很频繁）
                var countProgress = new Progress<int>(p => Volatile.Write(ref _latestDownloadedCount, p));

                DateTimeOffset? minCreatedUtc = null;
                if (UseCutoffDate && CutoffDate.HasValue)
                {
                    var localDate = DateTime.SpecifyKind(CutoffDate.Value.Date, DateTimeKind.Local);
                    minCreatedUtc = new DateTimeOffset(localDate).ToUniversalTime();
                    EnqueueLog($"启用截止日期(UTC): {minCreatedUtc:yyyy-MM-dd HH:mm:ss}");
                }

                var summary = await _redditApp.DownloadUserAsync(
                    Username,
                    IsVideoMode,
                    _settings.MaxConcurrentDownloads,
                    minCreatedUtc,
                    logProgress,
                    countProgress,
                    _cts.Token);

                if (_cts.IsCancellationRequested)
                    EnqueueLog("下载已取消.");
                else
                    EnqueueLog($"任务完成： 下载={summary.Success}，跳过={summary.Skip}，失败={summary.Fail}");
            }
            catch (OperationCanceledException)
            {
                EnqueueLog("下载已取消.");
            }
            catch (Exception ex)
            {
                EnqueueLog($"异常: {ex.Message}");
            }
            finally
            {
                IsDownloading = false;
                _cts?.Dispose();
                _cts = null;
            }
        }

        public async Task CheckLoginStatusAsync()
        {
            try
            {
                var token = await _redditAuth.GetAccessTokenAsync();
                if (!string.IsNullOrEmpty(token))
                {
                    IsLoggedIn = true;
                    EnqueueLog("[状态]: 自动登录成功.");
                }
            }
            catch (Exception ex)
            {
                IsLoggedIn = false;
                EnqueueLog($"[状态]: 自动登录失败: {ex.Message}");
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
