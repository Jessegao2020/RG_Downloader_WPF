using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Microsoft.Extensions.DependencyInjection;
using RedgifsDownloader.Avalonia.ViewModels;

namespace RedgifsDownloader.Avalonia;

public sealed class MainWindowViewModel : INotifyPropertyChanged
{
    private object? _currentView;

    public MainWindowViewModel()
    {
        DownloadsViewModel = new DownloadsViewModel();
        SettingsViewModel = new SettingsViewModel();
        RedditViewModel = Program.Services.GetRequiredService<RedditViewModel>();
        ImageSimilarityViewModel = Program.Services.GetRequiredService<ImageSimilarityViewModel>();
        DupePicsCleanerViewModel = Program.Services.GetRequiredService<DupePicsCleanerViewModel>();
        CurrentView = DownloadsViewModel;
        NavigateCommand = new RelayCommand(Navigate);
    }

    public DownloadsViewModel DownloadsViewModel { get; }
    public SettingsViewModel SettingsViewModel { get; }
    public RedditViewModel RedditViewModel { get; }
    public ImageSimilarityViewModel ImageSimilarityViewModel { get; }
    public DupePicsCleanerViewModel DupePicsCleanerViewModel { get; }
    public object? CurrentView { get => _currentView; private set => SetField(ref _currentView, value); }
    public ICommand NavigateCommand { get; }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void Navigate(object? parameter)
    {
        var target = parameter?.ToString();
        CurrentView = target switch
        {
            "Redgifs" or "Fikfap" => DownloadsViewModel,
            "Reddit" => RedditViewModel,
            "ImageSim" => ImageSimilarityViewModel,
            "Cleaner" => DupePicsCleanerViewModel,
            "Settings" => SettingsViewModel,
            "About" => "Redgifs Downloader (Avalonia)\nThis page is not implemented yet",
            _ => "Unknown view"
        };
    }

    private void SetField<T>(ref T field, T value, [CallerMemberName] string? name = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return;
        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
