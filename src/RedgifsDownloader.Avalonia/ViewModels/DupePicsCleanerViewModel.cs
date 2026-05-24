using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using RedgifsDownloader.ApplicationLayer.DupeCleaner;

namespace RedgifsDownloader.Avalonia.ViewModels;

public sealed class DupePicsCleanerViewModel : INotifyPropertyChanged
{
    private readonly DupeCleanerAppService _app;
    private bool _isBusy;
    private string _targetFolder = string.Empty;
    private string _resultMessage = string.Empty;

    public DupePicsCleanerViewModel(DupeCleanerAppService app)
    {
        _app = app;
        DeleteCommand = new AsyncCommand(DeleteAsync, CanRunActions);
        RenameCommand = new AsyncCommand(RenameFiles, CanRunActions);
    }

    public bool IsBusy
    {
        get => _isBusy;
        private set
        {
            if (SetField(ref _isBusy, value))
            {
                RaiseCommandsCanExecuteChanged();
            }
        }
    }

    public string TargetFolder
    {
        get => _targetFolder;
        set
        {
            if (SetField(ref _targetFolder, value))
            {
                RaiseCommandsCanExecuteChanged();
            }
        }
    }

    public string ResultMessage
    {
        get => _resultMessage;
        set => SetField(ref _resultMessage, value);
    }

    public ICommand DeleteCommand { get; }
    public ICommand RenameCommand { get; }

    public event PropertyChangedEventHandler? PropertyChanged;

    public void SetTargetFolder(string path)
    {
        if (!string.IsNullOrWhiteSpace(path))
        {
            TargetFolder = path;
        }
    }

    private async Task DeleteAsync()
    {
        if (string.IsNullOrWhiteSpace(TargetFolder)) return;

        IsBusy = true;
        ResultMessage = "正在分析并清理中…";

        try
        {
            var (_, _, logs) = await _app.CleanAsync(TargetFolder);
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

    private bool CanRunActions() => !IsBusy && !string.IsNullOrWhiteSpace(TargetFolder);

    private void RaiseCommandsCanExecuteChanged()
    {
        (DeleteCommand as AsyncCommand)?.RaiseCanExecuteChanged();
        (RenameCommand as AsyncCommand)?.RaiseCanExecuteChanged();
    }

    private bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return false;
        }

        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        return true;
    }
}
