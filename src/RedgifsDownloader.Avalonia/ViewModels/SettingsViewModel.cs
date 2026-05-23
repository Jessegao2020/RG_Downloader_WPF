using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Microsoft.Extensions.DependencyInjection;
using RedgifsDownloader.ApplicationLayer.Settings;

namespace RedgifsDownloader.Avalonia.ViewModels;

public sealed class SettingsViewModel : INotifyPropertyChanged
{
    private readonly IAppSettings _appSettings;
    private string _downloadDirectory = string.Empty;
    private int _maxConcurrentDownloads = 3;
    private string _saveStatus = string.Empty;

    public SettingsViewModel()
    {
        if (Avalonia.Controls.Design.IsDesignMode)
        {
            SaveCommand = new RelayCommand(_ => { });
            return;
        }

        _appSettings = Program.Services.GetRequiredService<IAppSettings>();
        _downloadDirectory = _appSettings.DownloadDirectory;
        _maxConcurrentDownloads = ClampConcurrent(_appSettings.MaxConcurrentDownloads);

        SaveCommand = new RelayCommand(_ => Save());
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

    public event PropertyChangedEventHandler? PropertyChanged;

    private void Save()
    {
        _appSettings.DownloadDirectory = string.IsNullOrWhiteSpace(DownloadDirectory)
            ? _appSettings.DownloadDirectory
            : DownloadDirectory.Trim();
        _appSettings.MaxConcurrentDownloads = ClampConcurrent(MaxConcurrentDownloads);
        _appSettings.Save();

        DownloadDirectory = _appSettings.DownloadDirectory;
        MaxConcurrentDownloads = _appSettings.MaxConcurrentDownloads;
        SaveStatus = "设置已保存";
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
