using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Ookii.Dialogs.Wpf;
using RedgifsDownloader.Helpers;
using RedgifsDownloader.Services.DupeCleaner;

namespace RedgifsDownloader.ViewModel
{
    public class DupePicsCleanerViewModel : INotifyPropertyChanged
    {
        private readonly DupeCleanerService _cleanerService;
        private readonly RenameService _renameService;

        public ICommand BrowseCommand { get; }
        public ICommand DeleteCommand { get; }
        public ICommand RenameCommand { get; }

        private bool _isBusy;
        private string _targetFolder = "";
        private string _resultMessage = "";

        public bool IsBusy
        {
            get => _isBusy;
            set { _isBusy = value; OnPropertyChanged(); }
        }
        public string TargetFolder
        {
            get => _targetFolder;
            set { _targetFolder = value; OnPropertyChanged(); (DeleteCommand as RelayCommand)?.RaiseCanExecuteChanged(); }
        }
        public string ResultMessage
        {
            get => _resultMessage;
            set { _resultMessage = value; OnPropertyChanged(); }
        }

        public DupePicsCleanerViewModel(DupeCleanerService cleanerService, RenameService renameService)
        {
            _cleanerService = cleanerService;
            _renameService = renameService;

            BrowseCommand = new RelayCommand(_ => BrowseFolder());
            DeleteCommand = new RelayCommand(async _ => await DeleteAsync(), _ => !string.IsNullOrWhiteSpace(TargetFolder));
            RenameCommand = new RelayCommand(async _ => await RenameFiles());
        }

        private void BrowseFolder()
        {
            var dialog = new VistaFolderBrowserDialog();
            dialog.Description = "选择图片所在文件夹";
            if (dialog.ShowDialog() == true)
            {
                TargetFolder = dialog.SelectedPath;
            }
        }

        private async Task DeleteAsync()
        {
            if (string.IsNullOrWhiteSpace(TargetFolder)) return;

            IsBusy = true;
            ResultMessage = "正在分析并清理中…";

            try
            {
                var (kept, deleted, logs) = await _cleanerService.CleanAsync(TargetFolder);
                ResultMessage = string.Join("\n", logs);
            }
            catch (Exception ex)
            {
                ResultMessage = $"发生错误: {ex.Message}";
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task RenameFiles()
        {
            if (string.IsNullOrWhiteSpace(TargetFolder)) return;
            IsBusy = true;
            ResultMessage = "正在重命名文件…";

            try
            {
                var (renamed, logs) = await _renameService.RenameAsync(TargetFolder);
                ResultMessage = $"重命名完成，共 {renamed} 个文件。\n" + string.Join("\n", logs);
            }
            catch (Exception ex)
            {
                ResultMessage = $"重命名时发生错误: {ex.Message}";
            }
            finally
            {
                IsBusy = false;
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
