using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text;
using System.Windows.Input;
using Avalonia;
using Avalonia.Threading;
using RedgifsDownloader.ApplicationLayer.Reddit;
using RedgifsDownloader.ApplicationLayer.Settings;
using RedgifsDownloader.Domain.Interfaces;

namespace RedgifsDownloader.Avalonia.ViewModels;

public sealed class RedditViewModel : INotifyPropertyChanged
{
    private readonly IRedditAuthService _redditAuth;
    private readonly IRedditDownloadAppService _redditApp;
    private readonly IAppSettings _settings;
    private readonly StringBuilder _logBuilder = new();

    private CancellationTokenSource? _cts;
    private bool _isLoggedIn;
    private bool _isVideoMode;
    private bool _isDownloading;
    private bool _useCutoffDate;
    private string _username = string.Empty;
    private int _downloadCount;
    private string _logContent = string.Empty;
    private DateTimeOffset? _cutoffDate = DateTimeOffset.Now.Date;

    public RedditViewModel()
    {
        if (!Design.IsDesignMode)
        {
            throw new InvalidOperationException("RedditViewModel requires DI services.");
        }

        _redditAuth = null!;
        _redditApp = null!;
        _settings = null!;
        LoginCommand = new AsyncCommand(() => Task.CompletedTask);
        DownloadCommand = new AsyncCommand(() => Task.CompletedTask);
    }

    public RedditViewModel(IRedditAuthService redditAuth, IRedditDownloadAppService redditApp, IAppSettings settings)
    {
        _redditAuth = redditAuth;
        _redditApp = redditApp;
        _settings = settings;

        LoginCommand = new AsyncCommand(LoginAsync, () => !IsLoggedIn);
        DownloadCommand = new AsyncCommand(ToggleDownloadAsync, () => IsLoggedIn && (!string.IsNullOrWhiteSpace(Username) || IsDownloading));

        _ = CheckLoginStatusAsync();
    }

    public string LoginBtnText => IsLoggedIn ? "Logged In" : "Login";
    public string DownloadBtnText => IsDownloading ? "Stop" : "Download";

    public bool IsLoggedIn
    {
        get => _isLoggedIn;
        private set
        {
            if (!SetField(ref _isLoggedIn, value)) return;
            OnPropertyChanged(nameof(LoginBtnText));
            RaiseCommandStates();
        }
    }

    public bool IsVideoMode
    {
        get => _isVideoMode;
        set
        {
            if (!SetField(ref _isVideoMode, value)) return;
            OnPropertyChanged(nameof(IsImageMode));
        }
    }

    public bool IsImageMode
    {
        get => !IsVideoMode;
        set
        {
            if (value)
            {
                IsVideoMode = false;
            }
        }
    }

    public bool IsDownloading
    {
        get => _isDownloading;
        private set
        {
            if (!SetField(ref _isDownloading, value)) return;
            OnPropertyChanged(nameof(DownloadBtnText));
            RaiseCommandStates();
        }
    }

    public bool UseCutoffDate { get => _useCutoffDate; set => SetField(ref _useCutoffDate, value); }
    public DateTimeOffset? CutoffDate { get => _cutoffDate; set => SetField(ref _cutoffDate, value); }

    public string Username
    {
        get => _username;
        set
        {
            if (!SetField(ref _username, value)) return;
            RaiseCommandStates();
        }
    }

    public int DownloadCount { get => _downloadCount; private set => SetField(ref _downloadCount, value); }
    public string LogContent { get => _logContent; private set => SetField(ref _logContent, value); }

    public ICommand LoginCommand { get; }
    public ICommand DownloadCommand { get; }

    public event PropertyChangedEventHandler? PropertyChanged;

    private async Task LoginAsync()
    {
        var ok = await _redditApp.LoginAsync();
        IsLoggedIn = ok;
        AppendLog(ok ? "登陆成功." : "登陆失败.");
    }

    private async Task CheckLoginStatusAsync()
    {
        try
        {
            var token = await _redditAuth.GetAccessTokenAsync();
            IsLoggedIn = !string.IsNullOrWhiteSpace(token);
            if (IsLoggedIn)
            {
                AppendLog("[状态]: 自动登录成功.");
            }
        }
        catch (Exception ex)
        {
            IsLoggedIn = false;
            AppendLog($"[状态]: 自动登录失败: {ex.Message}");
        }
    }

    private async Task ToggleDownloadAsync()
    {
        if (IsDownloading)
        {
            _cts?.Cancel();
            AppendLog("正在停止...");
            return;
        }

        IsDownloading = true;
        DownloadCount = 0;
        _cts = new CancellationTokenSource();

        try
        {
            AppendLog("开始下载...");

            DateTimeOffset? minCreatedUtc = null;
            if (UseCutoffDate && CutoffDate.HasValue)
            {
                minCreatedUtc = CutoffDate.Value.ToUniversalTime();
                AppendLog($"启用截止日期(UTC): {minCreatedUtc:yyyy-MM-dd HH:mm:ss}");
            }

            var logProgress = new Progress<string>(AppendLog);
            var countProgress = new Progress<int>(count => DownloadCount = count);

            var summary = await _redditApp.DownloadUserAsync(
                Username,
                IsVideoMode,
                _settings.MaxConcurrentDownloads,
                minCreatedUtc,
                logProgress,
                countProgress,
                _cts.Token);

            AppendLog(_cts.IsCancellationRequested
                ? "下载已取消."
                : $"任务完成： 下载={summary.Success}，跳过={summary.Skip}，失败={summary.Fail}");
        }
        catch (OperationCanceledException)
        {
            AppendLog("下载已取消.");
        }
        catch (Exception ex)
        {
            AppendLog($"异常: {ex.Message}");
        }
        finally
        {
            IsDownloading = false;
            _cts?.Dispose();
            _cts = null;
        }
    }

    private void AppendLog(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return;
        }

        Dispatcher.UIThread.Post(() =>
        {
            _logBuilder.AppendLine(message);
            LogContent = _logBuilder.ToString();
        });
    }

    private bool SetField<T>(ref T field, T value, [CallerMemberName] string? name = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        return true;
    }

    private void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

    private void RaiseCommandStates()
    {
        (LoginCommand as AsyncCommand)?.RaiseCanExecuteChanged();
        (DownloadCommand as AsyncCommand)?.RaiseCanExecuteChanged();
    }
}
