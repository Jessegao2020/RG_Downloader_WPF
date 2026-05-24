using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using RedgifsDownloader.Avalonia.ViewModels;
using Avalonia.Platform.Storage;

namespace RedgifsDownloader.Avalonia.Views;

public partial class ImageSimilarityView : UserControl
{
    public ImageSimilarityView()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private async void SelectFolder_OnClick(object? sender, RoutedEventArgs e)
    {
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel?.StorageProvider is null)
        {
            return;
        }

        var folders = await topLevel.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = "选择图片文件夹",
            AllowMultiple = false
        });

        var folder = folders.FirstOrDefault();
        var path = folder?.TryGetLocalPath();

        if (DataContext is ImageSimilarityViewModel vm && !string.IsNullOrWhiteSpace(path))
        {
            vm.SetFolderPath(path);
        }
    }

    private void LogBox_OnTextChanged(object? sender, TextChangedEventArgs e)
    {
        if (sender is TextBox logBox)
        {
            logBox.CaretIndex = logBox.Text?.Length ?? 0;
        }
    }
}
