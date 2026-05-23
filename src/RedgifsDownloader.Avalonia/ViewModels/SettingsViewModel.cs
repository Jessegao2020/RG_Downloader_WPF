using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;
using Microsoft.Extensions.DependencyInjection;
using RedgifsDownloader.ApplicationLayer.Settings;

namespace RedgifsDownloader.Avalonia.ViewModels;

public sealed class SettingsViewModel : INotifyPropertyChanged
{
    private readonly IAppSettings? _appSettings;
    private string _downloadDirectory = string.Empty;
    private int _maxConcurrentDownloads = 3;
    private string _saveStatus = string.Empty;

    public SettingsViewModel()
    {
        if (Design.IsDesignMode)
        {
            SaveCommand = new RelayCommand(_ => { });
            BrowseFolderCommand = new RelayCommand(_ => { });
            return;
        }

        _appSettings = Program.Services.GetRequiredService<IAppSettings>();
        _downloadDirectory = _appSettings.DownloadDirectory;
        _maxConcurrentDownloads = ClampConcurrent(_appSettings.MaxConcurrentDownloads);

        SaveCommand = new RelayCommand(_ => Save());
        BrowseFolderCommand = new AsyncCommand(BrowseFolderAsync);
    }

    public string DownloadDirectory
    {
        get => _downloadDirectory;
        set => SetField(ref _downloadDirectory, value);
    }

    public int MaxConcurrentDownloads
    {
        get => _maxConcurrentDownloads;
        set => SetField(ref _maxConcurrentDownloads, ClampConcurrent(value));
    }

    public string SaveStatus
    {
        get => _saveStatus;
        private set => SetField(ref _saveStatus, value);
    }

    public ICommand SaveCommand { get; }
    public ICommand BrowseFolderCommand { get; }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void Save()
    {
        if (_appSettings is null)
        {
            SaveStatus = "Design mode";
            return;
        }

        _appSettings.DownloadDirectory = string.IsNullOrWhiteSpace(DownloadDirectory)
            ? _appSettings.DownloadDirectory
            : DownloadDirectory.Trim();
        _appSettings.MaxConcurrentDownloads = ClampConcurrent(MaxConcurrentDownloads);
        _appSettings.Save();

        DownloadDirectory = _appSettings.DownloadDirectory;
        MaxConcurrentDownloads = _appSettings.MaxConcurrentDownloads;
        SaveStatus = "设置已保存";
    }



    private async Task BrowseFolderAsync()
    {
        if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
        {
            return;
        }

        var topLevel = desktop.MainWindow;
        if (topLevel is null)
        {
            return;
        }

        var folders = await topLevel.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = "选择下载目录",
            AllowMultiple = false
        });

        var folder = folders.FirstOrDefault();
        if (folder is null)
        {
            return;
        }

        var path = folder.TryGetLocalPath();
        if (!string.IsNullOrWhiteSpace(path))
        {
            DownloadDirectory = path;
        }
    }

    private static int ClampConcurrent(int value) => Math.Clamp(value, 1, 20);

    private bool SetField<T>(ref T field, T value, [CallerMemberName] string? name = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return false;
        }

        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        return true;
    }
}
