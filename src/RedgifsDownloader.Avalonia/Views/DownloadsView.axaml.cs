using System.Linq;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using RedgifsDownloader.Avalonia.ViewModels;

namespace RedgifsDownloader.Avalonia.Views;

public partial class DownloadsView : UserControl
{
    private VideoRow? _lastAdvancedClickedRow;

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


    private void ThumbnailCard_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (DataContext is not DownloadsViewModel viewModel || sender is not Border border || border.DataContext is not VideoRow row)
        {
            return;
        }

        if (IsPointerOnCheckbox(e.Source))
        {
            _lastAdvancedClickedRow = row;
            return;
        }

        if (e.KeyModifiers.HasFlag(KeyModifiers.Shift) && _lastAdvancedClickedRow is not null)
        {
            viewModel.SelectRange(_lastAdvancedClickedRow, row);
        }
        else
        {
            viewModel.ToggleSelection(row);
        }

        _lastAdvancedClickedRow = row;
        e.Handled = true;
    }

    private static bool IsPointerOnCheckbox(object? source)
    {
        if (source is not Visual visual)
        {
            return false;
        }

        for (var current = visual; current is not null; current = current.VisualParent)
        {
            if (current is CheckBox)
            {
                return true;
            }
        }

        return false;
    }

    private void UserBox_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter)
        {
            return;
        }

        if (DataContext is not DownloadsViewModel viewModel)
        {
            return;
        }

        if (viewModel.CrawlCommand.CanExecute(null))
        {
            viewModel.CrawlCommand.Execute(null);
        }

        e.Handled = true;
    }
}
