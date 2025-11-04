using Ookii.Dialogs.Wpf;
using RedgifsDownloader.Helpers;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Security.RightsManagement;
using System.Windows.Input;

namespace RedgifsDownloader.ViewModel
{
	public class ImageSimilarityViewModel : INotifyPropertyChanged
    {
        private string _folderPath;

        public ICommand GetDirectoryCommand { get; }

        public string FolderPath
        {
            get { return _folderPath; }
            set { _folderPath = value; OnPropertyChanged(); }
        }

        public ImageSimilarityViewModel()
        {
            GetDirectoryCommand = new RelayCommand(_=>GetDirectory());
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

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? name = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
