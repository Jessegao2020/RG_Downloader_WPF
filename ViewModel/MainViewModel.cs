using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using RedgifsDownloader.Helpers;
using RedgifsDownloader.Model;
using RedgifsDownloader.Services;
using Microsoft.Extensions.DependencyInjection;

namespace RedgifsDownloader.ViewModel
{
    public class MainViewModel : INotifyPropertyChanged
    {
        public ICommand NavigateCommand { get; }

        private object? _currentView;
        public object? CurrentView
        {
            get => _currentView;
            set { _currentView = value; OnPropertyChanged(); }
        }

        public MainViewModel()
        {
            NavigateCommand = new RelayCommand(param => Navigate(param?.ToString()));
            Navigate("Download"); // 默认打开主页
        }

        private void Navigate(string? page)
        {
            CurrentView = page switch
            {
                "Settings" => new SettingsViewModel(),
                "Download" => new DownloadsViewModel(new Services.DownloadCoordinator(
                                                         new Services.DownloadWorker(),
                                                         new Services.VideoFileService()),
                                                     new Services.CrawlService()),
                _ => CurrentView
            };
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
