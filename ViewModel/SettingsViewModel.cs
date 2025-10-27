using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Ookii.Dialogs.Wpf;
using RedgifsDownloader.Helpers;
using RedgifsDownloader.Interfaces;

namespace RedgifsDownloader.ViewModel
{
    public class SettingsViewModel : INotifyPropertyChanged
    {
        private readonly ISettingsService _settingsService;
        
        public string DownloadDirectory
        {
            get => _settingsService.DownloadDirectory;
            set
            {
                if(_settingsService.DownloadDirectory != value)
                {
                    _settingsService.DownloadDirectory = value;
                    _settingsService.Save();
                    OnPropertyChanged();
                }
            }
        }

        public ICommand SaveCommand { get; }
        public ICommand ChooseDownloadFolderCommand { get; }

        public SettingsViewModel(ISettingsService settingsService)
        {
            _settingsService = settingsService;
            ChooseDownloadFolderCommand = new RelayCommand(_ => GetDownloadDirectory());
        }

        public void GetDownloadDirectory()
        {
            var dialog = new VistaFolderBrowserDialog();
            dialog.Description = "选择文件夹";

            if (dialog.ShowDialog() == true)
            {
                DownloadDirectory = dialog.SelectedPath;
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? name = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
