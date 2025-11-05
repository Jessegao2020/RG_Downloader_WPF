using Ookii.Dialogs.Wpf;
using RedgifsDownloader.Helpers;
using RedgifsDownloader.Interfaces;
using RedgifsDownloader.Services.ImageSim;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;

namespace RedgifsDownloader.ViewModel
{
    public class ImageSimilarityViewModel : INotifyPropertyChanged
    {
        private readonly ILogService _logService;

        public ICommand GetDirectoryCommand { get; }
        public ICommand GoToFolderCommand { get; }
        public ICommand ExecuteCommand { get; }

        private string _folderPath = "";
        private double _threshold = 0.98;
        private double _progressValue;
        private string _logger = "";

        public string FolderPath
        {
            get { return _folderPath; }
            set { _folderPath = value; OnPropertyChanged(); }
        }
        public double Threshold
        {
            get { return _threshold; }
            set { _threshold = Math.Round(value, 2); OnPropertyChanged(); }
        }
        public double ProgressValue
        {
            get { return _progressValue; }
            set { _progressValue = value; OnPropertyChanged(); }
        }
        public string Logger
        {
            get { return _logger; }
            set { _logger = value; OnPropertyChanged(); }
        }

        public ImageSimilarityViewModel(ILogService logService)
        {
            _logService = logService;

            GetDirectoryCommand = new RelayCommand(_=>GetDirectory());
            GoToFolderCommand = new RelayCommand(_=> OpenDupeFolder());
            ExecuteCommand = new RelayCommand(async _=>await ExecuteAsync());
        }

        public void GetDirectory()
        {
            var dialog = new VistaFolderBrowserDialog();
            dialog.Description = "选择文件夹";

            if(dialog.ShowDialog() == true )
            {
                FolderPath = dialog.SelectedPath;
            }
        }

        private async Task ExecuteAsync()
        {
            if (string.IsNullOrWhiteSpace(FolderPath) || !Directory.Exists(FolderPath))
            {
                _logService.ShowMessage("请输入有效的文件夹");
                return;
            }

            Logger = "";
            Logger += $"开始检测目录: {FolderPath}\n";
            var sw = Stopwatch.StartNew();

            Progress<(int done, int total)> progress = new(p =>
            {
                ProgressValue = (double)p.done / p.total * 100;
            });

            var hashes = await Task.Run(() => ImageHashService.GetImageHashes(FolderPath, progress, msg => Logger+=msg + "\n"));

            var groups = await Task.Run(() => ImageHashService.GroupSimilarImages(hashes, Threshold));
            int dupCount = 0;
            foreach (var g in groups.Values) { dupCount += g.Count; }
            Logger += $"检测到 {groups.Count} 组相似图片，共 {dupCount} 张。\n";

            Logger += "正在移动重复文件到 dupe 文件夹...\n";
            await Task.Run(() => FileMoveService.MoveToDupeFolder(FolderPath, groups));

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
        private void OnPropertyChanged([CallerMemberName] string? name = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
