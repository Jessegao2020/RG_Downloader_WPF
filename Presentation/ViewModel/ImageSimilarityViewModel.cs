using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Ookii.Dialogs.Wpf;
using RedgifsDownloader.ApplicationLayer.ImageSimilarity;
using RedgifsDownloader.Domain.Interfaces;
using RedgifsDownloader.Helpers;

namespace RedgifsDownloader.Presentation.ViewModel
{
    public class ImageSimilarityViewModel : INotifyPropertyChanged
    {
        private readonly ILogService _log;
        private readonly IImageSimilarityAppService _imageSim;
        private readonly IDupeFileMoveService _dupeMover;

        public ICommand GetDirectoryCommand { get; }
        public ICommand GoToFolderCommand { get; }
        public ICommand ExecuteCommand { get; }

        private string _folderPath = "";
        private double _threshold = 0.98;
        private double _progressValue;
        private string _logger = "";

        public string FolderPath
        {
            get => _folderPath;
            set { _folderPath = value; OnPropertyChanged(); }
        }

        public double Threshold
        {
            get => _threshold;
            set { _threshold = Math.Round(value, 2); OnPropertyChanged(); }
        }

        public double ProgressValue
        {
            get => _progressValue;
            set { _progressValue = value; OnPropertyChanged(); }
        }

        public string Logger
        {
            get => _logger;
            set { _logger = value; OnPropertyChanged(); }
        }

        public ImageSimilarityViewModel(
            ILogService log,
            IImageSimilarityAppService imageSim,
            IDupeFileMoveService dupeMover)
        {
            _log = log;
            _imageSim = imageSim;
            _dupeMover = dupeMover;

            GetDirectoryCommand = new RelayCommand(_ => GetDirectory());
            GoToFolderCommand = new RelayCommand(_ => OpenDupeFolder());
            ExecuteCommand = new RelayCommand(async _ => await ExecuteAsync());
        }

        private void GetDirectory()
        {
            var dialog = new VistaFolderBrowserDialog
            {
                Description = "选择图片文件夹"
            };

            if (dialog.ShowDialog() == true)
            {
                FolderPath = dialog.SelectedPath;
            }
        }

        private async Task ExecuteAsync()
        {
            if (string.IsNullOrWhiteSpace(FolderPath) || !Directory.Exists(FolderPath))
            {
                _log.ShowMessage("请输入有效的文件夹");
                return;
            }

            Logger = $"开始检测目录: {FolderPath}\n";
            var sw = Stopwatch.StartNew();

            // UI Progress
            var progress = new Progress<(int done, int total)>(p =>
            {
                ProgressValue = (double)p.done / p.total * 100;
            });

            // 计算 hash —— 调用 ApplicationLayer，不再直接掉 Infra 静态方法
            var hashes = await Task.Run(() =>
                _imageSim.GetImageHashes(FolderPath, progress, msg => Logger += msg + "\n")
            );

            // 分组 —— 同样来自 ApplicationLayer
            var groups = await Task.Run(() =>
                _imageSim.GroupSimilarImages(hashes, Threshold)
            );

            int dupCount = groups.Values.Sum(g => g.Count);
            Logger += $"检测到 {groups.Count} 组相似图片，共 {dupCount} 张。\n";

            // 移动重复文件 —— 调用 Domain Service 实现
            Logger += "正在移动重复文件到 dupe 文件夹...\n";
            await Task.Run(() =>
                _dupeMover.MoveToDupeFolder(FolderPath, groups)
            );

            sw.Stop();
            Logger += $"已移动至 dupe 文件夹。耗时 {sw.Elapsed.TotalSeconds:F2} 秒。\n";
        }

        private void OpenDupeFolder()
        {
            if (string.IsNullOrWhiteSpace(FolderPath))
                return;

            string dupeDir = Path.Combine(FolderPath, "dupe");
            if (Directory.Exists(dupeDir))
                Process.Start("explorer.exe", dupeDir);
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
