using System.Windows.Input;
using Ookii.Dialogs.Wpf;
using RedgifsDownloader.Helpers;


namespace RedgifsDownloader.ViewModel
{
    public class SettingsViewModel
    {
        public string DownloadDirectory { get; set; }

        public ICommand SaveCommand { get; }
        public ICommand ChooseDownloadFolderCommand { get; }

        public SettingsViewModel()
        {
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
    }
}
