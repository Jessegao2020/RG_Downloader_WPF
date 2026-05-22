using System.Linq;
using Avalonia.Controls;
using Avalonia.Interactivity;
using RedgifsDownloader.Avalonia.ViewModels;

namespace RedgifsDownloader.Avalonia.Views;

public partial class DownloadsView : UserControl
{
    public DownloadsView()
    {
        InitializeComponent();
    }

    private void ActiveVideosGrid_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (DataContext is not DownloadsViewModel viewModel || sender is not DataGrid dataGrid)
        { 
            return;
        }

        viewModel.SyncSelectionFromDataGrid(dataGrid.SelectedItems.OfType<VideoRow>().ToList());
    }
}
