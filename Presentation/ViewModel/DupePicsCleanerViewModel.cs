using Ookii.Dialogs.Wpf;
using RedgifsDownloader.ApplicationLayer.DupeCleaner;
using RedgifsDownloader.Presentation.Helpers;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace RedgifsDownloader.Presentation.ViewModel
{
    public class DupePicsCleanerViewModel : INotifyPropertyChanged
    {
        private readonly DupeCleanerAppService _app;

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
            set
            {
                _targetFolder = value;
                OnPropertyChanged();
                (DeleteCommand as RelayCommand)?.RaiseCanExecuteChanged();
            }
        }

        public string ResultMessage
        {
            get => _resultMessage;
            set { _resultMessage = value; OnPropertyChanged(); }
        }

        public DupePicsCleanerViewModel(DupeCleanerAppService app)
        {
            _app = app;

            BrowseCommand = new RelayCommand(_ => BrowseFolder());
            DeleteCommand = new RelayCommand(async _ => await DeleteAsync(), _ => !string.IsNullOrWhiteSpace(TargetFolder));
            RenameCommand = new RelayCommand(async _ => await RenameFiles());
        }

        private void BrowseFolder()
        {
            var dlg = new VistaFolderBrowserDialog { Description = "选择图片所在文件夹" };
            if (dlg.ShowDialog() == true)
                TargetFolder = dlg.SelectedPath;
        }

        private async Task DeleteAsync()
        {
            if (string.IsNullOrWhiteSpace(TargetFolder)) return;

            IsBusy = true;
            ResultMessage = "正在分析并清理中…";

            try
            {
                var (kept, deleted, logs) = await _app.CleanAsync(TargetFolder);
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
                var (renamed, logs) = await _app.RenameAsync(TargetFolder);
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
