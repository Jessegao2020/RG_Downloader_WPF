using Microsoft.Extensions.DependencyInjection;
using RedgifsDownloader.Presentation.Helpers;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace RedgifsDownloader.Presentation.ViewModel
{
    public class MainViewModel : INotifyPropertyChanged
    {
        private readonly IServiceProvider _provider;
        public ICommand NavigateCommand { get; }

        private object? _currentView;
        public object? CurrentView
        {
            get => _currentView;
            set { _currentView = value; OnPropertyChanged(); }
        }

        public MainViewModel(IServiceProvider provider)
        {
            _provider = provider;
            NavigateCommand = new RelayCommand(param => Navigate(param?.ToString()));
            Navigate("Redgifs"); // 默认打开主页
        }

        private void Navigate(string? page)
        {
            CurrentView = page switch
            {
                "Settings" => _provider.GetRequiredService<SettingsViewModel>(),
                "Redgifs" => _provider.GetRequiredService<DownloadsViewModel>(),
                "Reddit" => _provider.GetRequiredService<RedditViewModel>(),
                "Cleaner" => _provider.GetRequiredService<DupePicsCleanerViewModel>(),
                "ImageSim" => _provider.GetRequiredService<ImageSimilarityViewModel>(),
                _ => CurrentView
            };
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
