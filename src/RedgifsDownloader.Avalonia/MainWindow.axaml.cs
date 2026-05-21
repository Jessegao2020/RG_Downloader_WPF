using System.Collections.ObjectModel;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using RedgifsDownloader.ApplicationLayer.Downloads;
using RedgifsDownloader.ApplicationLayer.Notifications;
using RedgifsDownloader.ApplicationLayer.Settings;
using RedgifsDownloader.Avalonia.Models;
using RedgifsDownloader.Domain.Entities;
using RedgifsDownloader.Domain.Enums;

namespace RedgifsDownloader.Avalonia;

public partial class MainWindow : Window
{
    private readonly IDownloadAppService _downloadAppService;
    private readonly IAppSettings _appSettings;
    private readonly VideoChangeNotifier _notifier;
    private readonly ObservableCollection<VideoRow> _rows = [];
    private readonly Dictionary<string, Video> _videosById = new();

    public MainWindow(IDownloadAppService downloadAppService, IAppSettings appSettings, VideoChangeNotifier notifier)
    {
        InitializeComponent();
        _downloadAppService = downloadAppService;
        _appSettings = appSettings;
        _notifier = notifier;

        OutputFolderTextBox.Text = Path.Combine(AppContext.BaseDirectory, "Downloads");
        ResultDataGrid.ItemsSource = _rows;

        _notifier.Subscribe(video => Dispatcher.UIThread.Post(() => UpdateVideoStatus(video)));
    }

    private async void OnCrawlClick(object? sender, RoutedEventArgs e)
    {
        try
        {
            var platform = GetPlatform();
            var username = UsernameTextBox.Text?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(username))
            {
                AppendLog("Username is required.");
                return;
            }

            _rows.Clear();
            _videosById.Clear();
            AppendLog($"Start crawl: {platform} / {username}");

            await foreach (var video in _downloadAppService.CrawlAsync(platform, username, msg => AppendLog($"ERROR: {msg}")))
            {
                _videosById[video.Id] = video;
                _rows.Add(ToRow(video));
            }

            AppendLog($"Crawl done. Total={_rows.Count}");
        }
        catch (Exception ex)
        {
            AppendLog($"Crawl failed: {ex.Message}");
        }
    }

    private async void OnDownloadClick(object? sender, RoutedEventArgs e)
    {
        try
        {
            if (_videosById.Count == 0)
            {
                AppendLog("No items to download. Please crawl first.");
                return;
            }

            if (!int.TryParse(MaxTextBox.Text, out var max) || max <= 0)
            {
                max = 3;
                MaxTextBox.Text = "3";
            }

            var output = (OutputFolderTextBox.Text ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(output))
            {
                AppendLog("Output folder is required.");
                return;
            }

            _appSettings.DownloadDirectory = Path.GetFullPath(output);
            Directory.CreateDirectory(_appSettings.DownloadDirectory);

            var videos = _videosById.Values.Take(max).ToList();
            foreach (var video in videos)
            {
                _notifier.RegisterVideo(video);
            }

            AppendLog($"Start download. Count={videos.Count}, Output={_appSettings.DownloadDirectory}");
            var summary = await _downloadAppService.DownloadAsync(videos, 1);
            AppendLog($"Download done. Completed={summary.Completed}, Failed={summary.Failed}");
        }
        catch (Exception ex)
        {
            AppendLog($"Download failed: {ex.Message}");
        }
    }

    private MediaPlatform GetPlatform()
    {
        var selected = (PlatformComboBox.SelectedItem as ComboBoxItem)?.Content?.ToString();
        return string.Equals(selected, "Fikfap", StringComparison.OrdinalIgnoreCase)
            ? MediaPlatform.Fikfap
            : MediaPlatform.Redgifs;
    }

    private void UpdateVideoStatus(Video video)
    {
        var row = _rows.FirstOrDefault(x => x.Id == video.Id);
        if (row is null) return;

        row.Status = video.Status.ToString();
        row.Progress = video.Progress.HasValue ? $"{video.Progress.Value:F1}%" : "-";
        ResultDataGrid.ItemsSource = null;
        ResultDataGrid.ItemsSource = _rows;
    }

    private VideoRow ToRow(Video video) => new()
    {
        Id = video.Id,
        Url = video.Url.ToString(),
        CreateDateRaw = video.CreateDateRaw,
        HasThumbnailUrl = !string.IsNullOrWhiteSpace(video.ThumbnailUrl),
        Status = video.Status.ToString(),
        Progress = video.Progress.HasValue ? $"{video.Progress.Value:F1}%" : "-"
    };

    private void AppendLog(string message)
    {
        LogTextBox.Text = $"{LogTextBox.Text}{DateTime.Now:HH:mm:ss} {message}{Environment.NewLine}";
        LogTextBox.CaretIndex = LogTextBox.Text?.Length ?? 0;
    }
}
