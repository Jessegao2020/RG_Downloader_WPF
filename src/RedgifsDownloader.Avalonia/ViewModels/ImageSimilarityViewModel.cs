using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Avalonia.Threading;
using RedgifsDownloader.ApplicationLayer.ImageSimilarity;
using RedgifsDownloader.ApplicationLayer.Interfaces;
using RedgifsDownloader.Domain.Interfaces;

namespace RedgifsDownloader.Avalonia.ViewModels;

public sealed class ImageSimilarityViewModel : INotifyPropertyChanged
{
    private readonly IUserNotificationService _log;
    private readonly IImageSimilarityAppService _imageSim;
    private readonly IDupeFileMoveService _dupeMover;

    private string _folderPath = string.Empty;
    private double _threshold = 0.98;
    private double _progressValue;
    private string _logger = string.Empty;
    private bool _isBusy;

    public ImageSimilarityViewModel(
        IUserNotificationService log,
        IImageSimilarityAppService imageSim,
        IDupeFileMoveService dupeMover)
    {
        _log = log;
        _imageSim = imageSim;
        _dupeMover = dupeMover;

        GetDirectoryCommand = new AsyncCommand(GetDirectoryAsync);
        GoToFolderCommand = new RelayCommand(_ => OpenDupeFolder());
        ExecuteCommand = new AsyncCommand(ExecuteAsync, () => !IsBusy);
    }

    public string FolderPath
    {
        get => _folderPath;
        set => SetField(ref _folderPath, value);
    }

    public double Threshold
    {
        get => _threshold;
        set => SetField(ref _threshold, Math.Round(value, 2));
    }

    public double ProgressValue
    {
        get => _progressValue;
        set => SetField(ref _progressValue, value);
    }

    public string Logger
    {
        get => _logger;
        set => SetField(ref _logger, value);
    }

    public bool IsBusy
    {
        get => _isBusy;
        private set
        {
            if (SetField(ref _isBusy, value))
            {
                (ExecuteCommand as AsyncCommand)?.RaiseCanExecuteChanged();
            }
        }
    }

    public ICommand GetDirectoryCommand { get; }
    public ICommand GoToFolderCommand { get; }
    public ICommand ExecuteCommand { get; }

    public event PropertyChangedEventHandler? PropertyChanged;

    public void SetFolderPath(string path)
    {
        if (!string.IsNullOrWhiteSpace(path))
        {
            FolderPath = path;
        }
    }

    private Task GetDirectoryAsync() => Task.CompletedTask;

    private async Task ExecuteAsync()
    {
        if (string.IsNullOrWhiteSpace(FolderPath) || !Directory.Exists(FolderPath))
        {
            _log.ShowMessage("请输入有效的文件夹");
            AppendLog("请输入有效的文件夹");
            return;
        }

        IsBusy = true;
        ProgressValue = 0;
        Logger = $"开始检测目录: {FolderPath}\n";

        var sw = Stopwatch.StartNew();
        try
        {
            var progress = new Progress<(int done, int total)>(p =>
            {
                ProgressValue = p.total <= 0 ? 0 : (double)p.done / p.total * 100;
            });

            var hashes = await Task.Run(() =>
                _imageSim.GetImageHashes(FolderPath, progress, msg => AppendLog(msg)));

            var groups = await Task.Run(() => _imageSim.GroupSimilarImages(hashes, Threshold));

            var dupCount = groups.Values.Sum(g => g.Count);
            AppendLog($"检测到 {groups.Count} 组相似图片，共 {dupCount} 张。");
            AppendLog("正在移动重复文件到 dupe 文件夹...");

            await Task.Run(() => _dupeMover.MoveToDupeFolder(FolderPath, groups));

            sw.Stop();
            AppendLog($"已移动至 dupe 文件夹。耗时 {sw.Elapsed.TotalSeconds:F2} 秒。");
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void OpenDupeFolder()
    {
        if (string.IsNullOrWhiteSpace(FolderPath))
        {
            return;
        }

        var dupeDir = Path.Combine(FolderPath, "dupe");
        if (!Directory.Exists(dupeDir))
        {
            _log.ShowMessage("dupe 文件夹不存在");
            return;
        }

        Process.Start(new ProcessStartInfo
        {
            FileName = dupeDir,
            UseShellExecute = true
        });
    }

    private void AppendLog(string message)
    {
        Dispatcher.UIThread.Post(() => Logger += message + "\n");
    }

    private bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return false;
        }

        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        return true;
    }
}
