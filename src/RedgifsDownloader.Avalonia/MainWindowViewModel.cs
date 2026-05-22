using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text;
using System.Windows.Input;
using Microsoft.Extensions.DependencyInjection;
using RedgifsDownloader.ApplicationLayer.Downloads;
using RedgifsDownloader.ApplicationLayer.Interfaces;
using RedgifsDownloader.ApplicationLayer.Notifications;
using RedgifsDownloader.Domain.Entities;
using RedgifsDownloader.Domain.Enums;

namespace RedgifsDownloader.Avalonia;

public sealed class MainWindowViewModel : INotifyPropertyChanged
{
    private readonly IDownloadAppService _downloadAppService;
    private readonly IAppSettings _appSettings;
    private readonly VideoChangeNotifier _notifier;
    private readonly List<Video> _videos = [];
    private readonly Dictionary<string, VideoRow> _rowsById = [];
    private readonly StringBuilder _logBuilder = new();

    private string _selectedPlatform = "Redgifs";
    private string _username = string.Empty;
    private string _maxCount = "3";
    private string _outputFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), "RG_Downloads");
    private string _logs = string.Empty;

    public MainWindowViewModel()
    {
        var provider = Program.Services;
        _downloadAppService = provider.GetRequiredService<IDownloadAppService>();
        _appSettings = provider.GetRequiredService<IAppSettings>();
        _notifier = provider.GetRequiredService<VideoChangeNotifier>();
        _notifier.Subscribe(OnVideoChanged);

        CrawlCommand = new AsyncCommand(CrawlAsync);
        DownloadCommand = new AsyncCommand(DownloadAsync);
    }

    public ObservableCollection<VideoRow> Items { get; } = [];
    public IReadOnlyList<string> PlatformOptions { get; } = ["Redgifs", "Fikfap"];

    public string SelectedPlatform { get => _selectedPlatform; set => SetField(ref _selectedPlatform, value); }
    public string Username { get => _username; set => SetField(ref _username, value); }
    public string MaxCount { get => _maxCount; set => SetField(ref _maxCount, value); }
    public string OutputFolder { get => _outputFolder; set => SetField(ref _outputFolder, value); }
    public string Logs { get => _logs; private set => SetField(ref _logs, value); }

    public ICommand CrawlCommand { get; }
    public ICommand DownloadCommand { get; }

    public event PropertyChangedEventHandler? PropertyChanged;

    private async Task CrawlAsync()
    {
        Items.Clear();
        _videos.Clear();
        _rowsById.Clear();

        var platform = ParsePlatform();
        AppendLog($"Start crawl: {platform}, username={Username}");

        await foreach (var video in _downloadAppService.CrawlAsync(platform, Username, AppendLog))
        {
            _videos.Add(video);
            if (_videos.Count > ParseMaxCount())
            {
                break;
            }

            var row = VideoRow.From(video);
            _rowsById[video.Id] = row;
            Items.Add(row);
            _notifier.RegisterVideo(video);
        }

        AppendLog($"Crawl done. Count={_videos.Count}");
    }

    private async Task DownloadAsync()
    {
        if (_videos.Count == 0)
        {
            AppendLog("No crawled items. Please crawl first.");
            return;
        }

        _appSettings.DownloadDirectory = Path.GetFullPath(OutputFolder);
        Directory.CreateDirectory(_appSettings.DownloadDirectory);

        AppendLog($"Start download. Output={_appSettings.DownloadDirectory}");
        var summary = await _downloadAppService.DownloadAsync(_videos, 1);
        AppendLog($"Download done. Completed={summary.Completed}, Failed={summary.Failed}");
    }

    private void OnVideoChanged(Video video)
    {
        if (_rowsById.TryGetValue(video.Id, out var row))
        {
            row.Update(video);
        }
    }

    private MediaPlatform ParsePlatform()
        => string.Equals(SelectedPlatform, "Fikfap", StringComparison.OrdinalIgnoreCase)
            ? MediaPlatform.Fikfap
            : MediaPlatform.Redgifs;

    private int ParseMaxCount()
        => int.TryParse(MaxCount, out var max) && max > 0 ? max : 3;

    private void AppendLog(string msg)
    {
        _logBuilder.AppendLine($"[{DateTime.Now:HH:mm:ss}] {msg}");
        Logs = _logBuilder.ToString();
    }

    private void SetField<T>(ref T field, T value, [CallerMemberName] string? name = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return;
        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}

public sealed class VideoRow : INotifyPropertyChanged
{
    private string _status = string.Empty;
    private string _progress = string.Empty;

    public string Id { get; private set; } = string.Empty;
    public string Url { get; private set; } = string.Empty;
    public long? CreateDateRaw { get; private set; }
    public bool HasThumbnailUrl { get; private set; }
    public string Status { get => _status; private set => SetField(ref _status, value); }
    public string Progress { get => _progress; private set => SetField(ref _progress, value); }

    public static VideoRow From(Video video)
    {
        var row = new VideoRow
        {
            Id = video.Id,
            Url = video.Url.ToString(),
            CreateDateRaw = video.CreateDateRaw,
            HasThumbnailUrl = !string.IsNullOrWhiteSpace(video.ThumbnailUrl)
        };
        row.Update(video);
        return row;
    }

    public void Update(Video video)
    {
        Status = video.Status.ToString();
        Progress = video.Progress?.ToString("F1") ?? "-";
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    private void SetField<T>(ref T field, T value, [CallerMemberName] string? name = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return;
        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}

public sealed class AsyncCommand(Func<Task> action) : ICommand
{
    private bool _isRunning;
    public event EventHandler? CanExecuteChanged;
    public bool CanExecute(object? parameter) => !_isRunning;

    public async void Execute(object? parameter)
    {
        if (_isRunning) return;
        _isRunning = true;
        CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        try { await action(); }
        finally
        {
            _isRunning = false;
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}
