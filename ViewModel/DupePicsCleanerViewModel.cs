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

        public DupePicsCleanerViewModel(DupeCleanerService cleanerService)
        {
            _cleanerService = cleanerService;
            BrowseCommand = new RelayCommand(_ => BrowseFolder());
            DeleteCommand = new RelayCommand(async _ => await DeleteAsync(), _ => !string.IsNullOrWhiteSpace(TargetFolder));
        }

        private string _targetFolder = "";
        public string TargetFolder
        {
            get => _targetFolder;
            set { _targetFolder = value; OnPropertyChanged(); (DeleteCommand as RelayCommand)?.RaiseCanExecuteChanged(); }
        }

        private bool _isBusy;
        public bool IsBusy
        {
            get => _isBusy;
            set { _isBusy = value; OnPropertyChanged(); }
        }

        private string _resultMessage = "";
        public string ResultMessage
        {
            get => _resultMessage;
            set { _resultMessage = value; OnPropertyChanged(); }
        }

        public ICommand BrowseCommand { get; }
        public ICommand DeleteCommand { get; }

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

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
