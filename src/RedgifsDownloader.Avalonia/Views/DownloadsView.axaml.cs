using System.Linq;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using RedgifsDownloader.Avalonia.ViewModels;

namespace RedgifsDownloader.Avalonia.Views;

public partial class DownloadsView : UserControl
{
    private int _lastClickedIndex = -1;

    public DownloadsView()
    {
        InitializeComponent();
    }

    private void ActiveVideosGrid_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (DataContext is not DownloadsViewModel viewModel)
        {
            return;
        }

        if (e.Source is not Control sourceControl)
        {
            return;
        }

        if (sourceControl.GetSelfAndVisualAncestors().OfType<CheckBox>().Any())
        {
            return;
        }

        var row = sourceControl.GetSelfAndVisualAncestors()
            .OfType<DataGridRow>()
            .Select(r => r.DataContext)
            .OfType<VideoRow>()
            .FirstOrDefault();

        row ??= sourceControl.DataContext as VideoRow;

        if (row is null)
        {
            return;
        }

        var items = viewModel.ActiveVideos.ToList();
        var currentIndex = items.IndexOf(row);
        if (currentIndex < 0)
        {
            return;
        }

        var isShiftPressed = e.KeyModifiers.HasFlag(KeyModifiers.Shift);
        if (isShiftPressed && _lastClickedIndex >= 0 && _lastClickedIndex < items.Count)
        {
            viewModel.SelectRange(items[_lastClickedIndex], row);
        }
        else
        {
            viewModel.ToggleSelection(row);
        }

        _lastClickedIndex = currentIndex;
    }
}
