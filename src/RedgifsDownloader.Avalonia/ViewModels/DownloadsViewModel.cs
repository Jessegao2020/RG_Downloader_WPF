using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Microsoft.Extensions.DependencyInjection;
using RedgifsDownloader.ApplicationLayer.Downloads;
using RedgifsDownloader.ApplicationLayer.Interfaces;
using RedgifsDownloader.ApplicationLayer.Notifications;
using RedgifsDownloader.ApplicationLayer.Settings;
using RedgifsDownloader.Domain.Entities;
using RedgifsDownloader.Domain.Enums;
using Avalonia.Controls;
using Avalonia.Media.Imaging;
using RedgifsDownloader.Avalonia.Services;

namespace RedgifsDownloader.Avalonia.ViewModels;

public sealed class DownloadsViewModel : INotifyPropertyChanged
{
    private readonly IDownloadAppService _downloadAppService;
    private readonly IAppSettings _appSettings;
    private readonly VideoChangeNotifier _notifier;
    private readonly ThumbnailLoader _thumbnailLoader;
    private readonly Dictionary<string, VideoRow> _rowsById = [];
    private string _selectedPlatform = "Redgifs";
    private string _username = string.Empty;
    private bool _isAdvancedMode;
    private bool _isCaptionVisible = true;
    private bool _isAllSelected;
    private bool _suppressAllSelectedSync;
    private bool _isCrawling;
    private bool _isDownloading;
    private string _statusMessage = string.Empty;
    private CancellationTokenSource? _cts;

    public DownloadsViewModel()
    {
        if (Design.IsDesignMode)
{
    _downloadAppService = null!;
    _appSettings = null!;
    _notifier = null!;
    _thumbnailLoader = null!;

    RetryAllCommand = new RelayCommand(_ => { });
    OpenDownloadFolderCommand = new RelayCommand(_ => { });
    SelectAllCommand = new RelayCommand(_ => { });
    DeselectAllCommand = new RelayCommand(_ => { });
    SortByNameCommand = new RelayCommand(_ => { });
    SortByDateCommand = new RelayCommand(_ => { });
    CrawlCommand = new RelayCommand(_ => { });
    DownloadCommand = new RelayCommand(_ => { });
    StopCommand = new RelayCommand(_ => { });

    return;
}

        var provider = Program.Services;
        _downloadAppService = provider.GetRequiredService<IDownloadAppService>();
        _appSettings = provider.GetRequiredService<IAppSettings>();
        _notifier = provider.GetRequiredService<VideoChangeNotifier>();
        _thumbnailLoader = provider.GetRequiredService<ThumbnailLoader>();
        _notifier.Subscribe(OnVideoChanged);
        CrawlCommand = new AsyncCommand(CrawlAsync, () => !IsCrawling && !IsDownloading);
        DownloadCommand = new AsyncCommand(DownloadAsync, () => !IsCrawling && !IsDownloading);
        StopCommand = new RelayCommand(_ => _cts?.Cancel(), () => IsCrawling || IsDownloading);
        RetryAllCommand = new AsyncCommand(RetryAllAsync, () => !IsCrawling && !IsDownloading);
        OpenDownloadFolderCommand = new RelayCommand(_ => OpenDownloadFolder());
        SelectAllCommand = new RelayCommand(_ => SetSelection(true));
        DeselectAllCommand = new RelayCommand(_ => SetSelection(false));
        SortByNameCommand = new RelayCommand(_ => Reorder(Videos.OrderBy(v => v.Id, StringComparer.OrdinalIgnoreCase)));
        SortByDateCommand = new RelayCommand(_ => Reorder(Videos.OrderByDescending(v => v.CreateDateRaw ?? long.MinValue)));
    }

    public ObservableCollection<VideoRow> Videos { get; } = [];
    public ObservableCollection<VideoRow> ActiveVideos { get; } = [];
    public ObservableCollection<VideoRow> FailedVideos { get; } = [];
    public IReadOnlyList<string> Platforms { get; } = ["Redgifs", "Fikfap"];
    public string SelectedPlatform { get => _selectedPlatform; set => SetField(ref _selectedPlatform, value); }
    public string Username { get => _username; set => SetField(ref _username, value); }
    public bool IsAdvancedMode { get => _isAdvancedMode; set => SetField(ref _isAdvancedMode, value); }
    public bool IsCaptionVisible { get => _isCaptionVisible; set => SetField(ref _isCaptionVisible, value); }
    public bool IsAllSelected
    {
        get => _isAllSelected;
        set
        {
            if (!SetField(ref _isAllSelected, value))
            {
                return;
            }

            if (_suppressAllSelectedSync)
            {
                return;
            }

            SetSelection(value);
        }
    }
    public bool IsCrawling { get => _isCrawling; private set { if (SetField(ref _isCrawling, value)) RaiseState(); } }
    public bool IsDownloading { get => _isDownloading; private set { if (SetField(ref _isDownloading, value)) RaiseState(); } }
    public string StatusMessage { get => _statusMessage; private set => SetField(ref _statusMessage, value); }
    public string CrawlBtnText => IsCrawling ? "Crawling..." : "Crawl";
    public string DownloadBtnText => IsDownloading ? "下载中..." : "下载";
    public int VideosCount => Videos.Count;
    public int CompletedCount => Videos.Count(v => v.Status.Contains("Completed", StringComparison.OrdinalIgnoreCase));
    public int FailedCount => FailedVideos.Count;
    public ICommand CrawlCommand { get; }
    public ICommand DownloadCommand { get; }
    public ICommand StopCommand { get; }
    public ICommand RetryAllCommand { get; }
    public ICommand OpenDownloadFolderCommand { get; }
    public ICommand SelectAllCommand { get; }
    public ICommand DeselectAllCommand { get; }
    public ICommand SortByNameCommand { get; }
    public ICommand SortByDateCommand { get; }
    public event PropertyChangedEventHandler? PropertyChanged;

    private async Task CrawlAsync()
    {
        Videos.Clear(); ActiveVideos.Clear(); FailedVideos.Clear(); _rowsById.Clear(); IsAllSelected = false; IsCrawling = true;
        try
        {
            _cts = new CancellationTokenSource();
            await foreach (var video in _downloadAppService.CrawlAsync(ParsePlatform(), Username, _ => { }, _cts.Token))
            {
                var row = VideoRow.From(video);
                _rowsById[video.Id] = row;
                Videos.Add(row);
                if (!IsFailedStatus(row.Status))
                {
                    ActiveVideos.Add(row);
                }
                else
                {
                    FailedVideos.Add(row);
                }

                _notifier.RegisterVideo(video);
                _ = _thumbnailLoader.LoadAsync(row);
            }
            StatusMessage = $"爬取完成，共 {Videos.Count} 条";
        }
        catch (OperationCanceledException)
        {
            StatusMessage = "爬取已取消";
        }
        finally
        {
            RefreshVisibleCollections();
            IsCrawling = false;
            _cts?.Dispose();
            _cts = null;
            RaiseCounts();
        }
    }

    private async Task DownloadAsync()
    {
        var selected = Videos.Where(v => v.IsSelected).Select(v => v.Item).ToList();
        if (selected.Count == 0) { StatusMessage = "请选择要下载的视频"; return; }
        IsDownloading = true; Directory.CreateDirectory(_appSettings.DownloadDirectory);
        try
        {
            _cts = new CancellationTokenSource();
            var summary = await _downloadAppService.DownloadAsync(selected, _appSettings.MaxConcurrentDownloads, _cts.Token);
            StatusMessage = $"下载完成：成功 {summary.Completed}，失败 {summary.Failed}";
            RefreshVisibleCollections(); RaiseCounts();
        }
        catch (OperationCanceledException)
        {
            foreach (var video in selected.Where(v => v.Status is not (VideoStatus.Completed or VideoStatus.Exists)))
            {
                video.MarkCanceled();
                if (_rowsById.TryGetValue(video.Id, out var row))
                {
                    row.Update(video);
                }
            }

            RefreshVisibleCollections();
            RaiseCounts();
            StatusMessage = "下载已取消";
        }
        finally
        {
            IsDownloading = false;
            _cts?.Dispose();
            _cts = null;
        }
    }

    private async Task RetryAllAsync() { foreach (var row in FailedVideos) row.IsSelected = true; await DownloadAsync(); }
    private void OpenDownloadFolder()
    {
        Directory.CreateDirectory(_appSettings.DownloadDirectory);
        Process.Start(new ProcessStartInfo
        {
            FileName = _appSettings.DownloadDirectory,
            UseShellExecute = true
        });
    }
    private void OnVideoChanged(Video video)
    {
        if (_rowsById.TryGetValue(video.Id, out var row))
        {
            row.Update(video);
            RefreshVisibleCollections();
            RaiseCounts();
        }
    }
    public void ToggleSelection(VideoRow row)
    {
        row.IsSelected = !row.IsSelected;
        RefreshSelectionState();
    }

    public void SelectRange(VideoRow start, VideoRow end)
    {
        var items = ActiveVideos.ToList();
        var startIndex = items.IndexOf(start);
        var endIndex = items.IndexOf(end);
        if (startIndex < 0 || endIndex < 0)
        {
            return;
        }

        var from = Math.Min(startIndex, endIndex);
        var to = Math.Max(startIndex, endIndex);
        for (var i = from; i <= to; i++)
        {
            items[i].IsSelected = true;
        }

        RefreshSelectionState();
    }

    public void SyncSelectionFromDataGrid(IReadOnlyCollection<VideoRow> selectedRows)
    {
        var selectedSet = selectedRows.ToHashSet();

        foreach (var row in ActiveVideos)
        {
            row.IsSelected = selectedSet.Contains(row);
        }

        RefreshSelectionState();
    }

    private void SetSelection(bool s)
    {
        foreach (var row in Videos)
        {
            row.IsSelected = s;
        }

        _suppressAllSelectedSync = true;
        IsAllSelected = s;
        _suppressAllSelectedSync = false;
    }

    private void RefreshSelectionState()
    {
        _suppressAllSelectedSync = true;
        IsAllSelected = ActiveVideos.Count > 0 && ActiveVideos.All(v => v.IsSelected);
        _suppressAllSelectedSync = false;
    }
    private void Reorder(IEnumerable<VideoRow> ordered)
    {
        var list = ordered.ToList();

        Videos.Clear();
        foreach (var item in list)
        {
            Videos.Add(item);
        }

        RefreshVisibleCollections();
        RefreshSelectionState();
        RaiseCounts();
    }
    private void RefreshVisibleCollections()
    {
        ActiveVideos.Clear();
        FailedVideos.Clear();
        foreach (var row in Videos)
        {
            if (IsFailedStatus(row.Status))
            {
                FailedVideos.Add(row);
                continue;
            }

            ActiveVideos.Add(row);
        }
    }

    private static bool IsFailedStatus(string status) =>
        status is nameof(VideoStatus.Failed)
            or nameof(VideoStatus.NetworkError)
            or nameof(VideoStatus.WriteError)
            or nameof(VideoStatus.UnknownError)
            or nameof(VideoStatus.Canceled);
    private MediaPlatform ParsePlatform() => string.Equals(SelectedPlatform, "Fikfap", StringComparison.OrdinalIgnoreCase) ? MediaPlatform.Fikfap : MediaPlatform.Redgifs;
    private void RaiseState() { OnPropertyChanged(nameof(CrawlBtnText)); OnPropertyChanged(nameof(DownloadBtnText)); (CrawlCommand as AsyncCommand)?.RaiseCanExecuteChanged(); (DownloadCommand as AsyncCommand)?.RaiseCanExecuteChanged(); (RetryAllCommand as AsyncCommand)?.RaiseCanExecuteChanged(); (StopCommand as RelayCommand)?.RaiseCanExecuteChanged(); }
    private void RaiseCounts() { OnPropertyChanged(nameof(VideosCount)); OnPropertyChanged(nameof(CompletedCount)); OnPropertyChanged(nameof(FailedCount)); }
    private bool SetField<T>(ref T field, T value, [CallerMemberName] string? n = null) { if (EqualityComparer<T>.Default.Equals(field, value)) return false; field = value; OnPropertyChanged(n); return true; }
    private void OnPropertyChanged([CallerMemberName] string? n = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));
}

public sealed class VideoRow : INotifyPropertyChanged
{
    private bool _isSelected; private string _status = string.Empty; private string _progress = string.Empty; private string _displayStatus = string.Empty;
    public bool IsSelected { get => _isSelected; set => SetField(ref _isSelected, value); }
    public string Id { get; private set; } = string.Empty;
    public string Url { get; private set; } = string.Empty;
    public long? CreateDateRaw { get; private set; }
    public string DisplayCreateDate { get; private set; } = "-";
    public bool HasThumbnailUrl { get; private set; }
    public string? ThumbnailUrl { get; private set; }
    private Bitmap? _thumbnailImage;
    public Bitmap? ThumbnailImage { get => _thumbnailImage; set => SetField(ref _thumbnailImage, value); }
    public string Status { get => _status; private set => SetField(ref _status, value); }
    public string Progress { get => _progress; private set => SetField(ref _progress, value); }
    public string DisplayStatus { get => _displayStatus; private set => SetField(ref _displayStatus, value); }
    public Video Item { get; private set; } = default!;
    public static VideoRow From(Video video)
    {
        var row = new VideoRow { Id = video.Id, Url = video.Url.ToString(), CreateDateRaw = video.CreateDateRaw, DisplayCreateDate = video.CreateDateRaw is long t ? DateTimeOffset.FromUnixTimeSeconds(t).ToLocalTime().ToString("yyyy-MM-dd HH:mm") : "-", HasThumbnailUrl = !string.IsNullOrWhiteSpace(video.ThumbnailUrl), ThumbnailUrl = video.ThumbnailUrl, Item = video };
        row.Update(video); return row;
    }
    public void Update(Video video)
    {
        Item = video;
        Status = video.Status.ToString();
        Progress = video.Progress?.ToString("F1") ?? "-";
        DisplayStatus = video.Status switch
        {
            VideoStatus.Pending => "",
            VideoStatus.Downloading => video.Progress.HasValue ? $"{video.Progress.Value:F1}%" : "下载中",
            VideoStatus.Completed => "完成",
            VideoStatus.Exists => "已存在",
            VideoStatus.Failed => "失败",
            VideoStatus.Canceled => "已取消",
            VideoStatus.NetworkError => "网络错误",
            VideoStatus.WriteError => "写入错误",
            VideoStatus.UnknownError => "未知错误",
            _ => ""
        };
    }
    public event PropertyChangedEventHandler? PropertyChanged;
    private void SetField<T>(ref T field, T value, [CallerMemberName] string? n = null) { if (EqualityComparer<T>.Default.Equals(field, value)) return; field = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n)); }
}

public sealed class AsyncCommand(Func<Task> action, Func<bool>? canExecute = null) : ICommand
{
    private bool _isRunning;
    public event EventHandler? CanExecuteChanged;
    public bool CanExecute(object? parameter) => !_isRunning && (canExecute?.Invoke() ?? true);
    public async void Execute(object? parameter) { if (!CanExecute(parameter)) return; _isRunning = true; RaiseCanExecuteChanged(); try { await action(); } finally { _isRunning = false; RaiseCanExecuteChanged(); } }
    public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
}

public sealed class RelayCommand(Action<object?> execute, Func<bool>? canExecute = null) : ICommand
{
    public event EventHandler? CanExecuteChanged;
    public bool CanExecute(object? parameter) => canExecute?.Invoke() ?? true;
    public void Execute(object? parameter) => execute(parameter);
    public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
}
