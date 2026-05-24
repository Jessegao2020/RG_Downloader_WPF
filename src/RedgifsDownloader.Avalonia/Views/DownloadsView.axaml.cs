using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using RedgifsDownloader.Avalonia.ViewModels;

namespace RedgifsDownloader.Avalonia.Views;

public partial class DownloadsView : UserControl
{
    private bool _isAdvancedDragging;
    private int _advancedDragStartIndex = -1;
    private int _advancedDragMinIndex = -1;
    private int _advancedDragMaxIndex = -1;
    private Point _advancedDragStartPoint;

    private int _lastAdvancedClickedIndex = -1;
    private bool _isAdvancedShiftSession;
    private int _advancedShiftStartIndex = -1;
    private int _advancedShiftMinIndex = -1;
    private int _advancedShiftMaxIndex = -1;

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

    private async Task CopyRowUrlAsync(VideoRow? row)
    {
        if (row is null || string.IsNullOrWhiteSpace(row.Url))
        {
            return;
        }

        try
        {
            await (TopLevel.GetTopLevel(this)?.Clipboard?.SetTextAsync(row.Url) ?? Task.CompletedTask);
            var owner = TopLevel.GetTopLevel(this) as Window;
            ToastWindow.Show(owner, $"已复制 URL:\n{row.Url}");
        }
        catch
        {
            var owner = TopLevel.GetTopLevel(this) as Window;
            ToastWindow.Show(owner, "复制 URL 失败");
        }
    }

    private async void ActiveVideosGrid_DoubleTapped(object? sender, TappedEventArgs e)
    {
        if (IsPointerOnCheckbox(e.Source))
        {
            return;
        }

        if (e.Source is not Control sourceControl)
        {
            return;
        }

        var row = sourceControl.GetSelfAndVisualAncestors()
            .OfType<DataGridRow>()
            .Select(r => r.DataContext)
            .OfType<VideoRow>()
            .FirstOrDefault();
        await CopyRowUrlAsync(row);
    }


    private void ThumbnailCard_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (DataContext is not DownloadsViewModel viewModel || sender is not Border border || border.DataContext is not VideoRow row)
        {
            return;
        }

        if (IsPointerOnCheckbox(e.Source))
        {
            return;
        }

        var currentIndex = viewModel.ActiveVideos.IndexOf(row);
        if (currentIndex < 0)
        {
            return;
        }

        if (e.KeyModifiers.HasFlag(KeyModifiers.Shift))
        {
            if (!_isAdvancedShiftSession)
            {
                _isAdvancedShiftSession = true;
                _advancedShiftStartIndex = _lastAdvancedClickedIndex >= 0 ? _lastAdvancedClickedIndex : currentIndex;
                _advancedShiftMinIndex = Math.Min(_advancedShiftStartIndex, currentIndex);
                _advancedShiftMaxIndex = Math.Max(_advancedShiftStartIndex, currentIndex);
                viewModel.BeginRangeSelect();
            }
            else
            {
                _advancedShiftMinIndex = Math.Min(_advancedShiftMinIndex, currentIndex);
                _advancedShiftMaxIndex = Math.Max(_advancedShiftMaxIndex, currentIndex);
            }

            viewModel.RangeSelect(_advancedShiftStartIndex, currentIndex, _advancedShiftMinIndex, _advancedShiftMaxIndex);
            _lastAdvancedClickedIndex = currentIndex;
            _advancedDragStartIndex = -1;
        }
        else
        {
            if (_isAdvancedShiftSession)
            {
                viewModel.EndRangeSelect();
                ResetShiftSession();
            }

            _advancedDragStartIndex = currentIndex;
            _advancedDragStartPoint = e.GetPosition(AdvancedThumbnailListBox);
            _isAdvancedDragging = false;
        }

        e.Handled = true;
    }

    private static bool IsPointerOnCheckbox(object? source)
    {
        if (source is not Control sourceControl)
        {
            return false;
        }

        return sourceControl.GetSelfAndVisualAncestors().OfType<CheckBox>().Any();
    }

    private VideoRow? FindVideoRowAtPointer(PointerEventArgs e)
    {
        var point = e.GetPosition(AdvancedThumbnailListBox);
        var hit = AdvancedThumbnailListBox.InputHitTest(point);

        if (hit is Control control)
        {
            return control.GetSelfAndVisualAncestors()
                       .OfType<Control>()
                       .Select(c => c.DataContext)
                       .OfType<VideoRow>()
                       .FirstOrDefault();
        }

        return null;
    }

    private void AdvancedThumbnailListBox_PointerMoved(object? sender, PointerEventArgs e)
    {
        if (DataContext is not DownloadsViewModel viewModel)
        {
            return;
        }

        if (!e.KeyModifiers.HasFlag(KeyModifiers.Shift) && _isAdvancedShiftSession)
        {
            viewModel.EndRangeSelect();
            ResetShiftSession();
        }

        var point = e.GetCurrentPoint(AdvancedThumbnailListBox);
        if (!point.Properties.IsLeftButtonPressed)
        {
            if (_isAdvancedDragging)
            {
                viewModel.EndRangeSelect();
            }

            ResetDragState();
            return;
        }

        if (_advancedDragStartIndex < 0)
        {
            return;
        }

        if (!_isAdvancedDragging)
        {
            var currentPoint = e.GetPosition(AdvancedThumbnailListBox);
            if (Math.Abs(currentPoint.X - _advancedDragStartPoint.X) <= 4 &&
                Math.Abs(currentPoint.Y - _advancedDragStartPoint.Y) <= 4)
            {
                return;
            }

            _isAdvancedDragging = true;
            _advancedDragMinIndex = _advancedDragStartIndex;
            _advancedDragMaxIndex = _advancedDragStartIndex;
            viewModel.BeginRangeSelect();
        }

        var row = FindVideoRowAtPointer(e);
        if (row is null)
        {
            return;
        }

        var currentIndex = viewModel.ActiveVideos.IndexOf(row);
        if (currentIndex < 0)
        {
            return;
        }

        _advancedDragMinIndex = Math.Min(_advancedDragMinIndex, Math.Min(_advancedDragStartIndex, currentIndex));
        _advancedDragMaxIndex = Math.Max(_advancedDragMaxIndex, Math.Max(_advancedDragStartIndex, currentIndex));
        viewModel.RangeSelect(_advancedDragStartIndex, currentIndex, _advancedDragMinIndex, _advancedDragMaxIndex);
    }

    private void AdvancedThumbnailListBox_PointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (DataContext is not DownloadsViewModel viewModel)
        {
            return;
        }

        if (_isAdvancedDragging)
        {
            viewModel.EndRangeSelect();
        }
        else if (_advancedDragStartIndex >= 0 && _advancedDragStartIndex < viewModel.ActiveVideos.Count)
        {
            var row = viewModel.ActiveVideos[_advancedDragStartIndex];
            row.IsSelected = !row.IsSelected;
            _lastAdvancedClickedIndex = _advancedDragStartIndex;
            viewModel.RefreshSelectionState();
        }

        if (!e.KeyModifiers.HasFlag(KeyModifiers.Shift) && _isAdvancedShiftSession)
        {
            viewModel.EndRangeSelect();
            ResetShiftSession();
        }

        ResetDragState();
    }

    private void AdvancedThumbnailListBox_PointerExited(object? sender, PointerEventArgs e)
    {
        if (DataContext is not DownloadsViewModel viewModel)
        {
            return;
        }

        if (_isAdvancedDragging)
        {
            viewModel.EndRangeSelect();
            ResetDragState();
        }
    }

    private void ResetShiftSession()
    {
        _isAdvancedShiftSession = false;
        _advancedShiftStartIndex = -1;
        _advancedShiftMinIndex = -1;
        _advancedShiftMaxIndex = -1;
    }

    private void ResetDragState()
    {
        _isAdvancedDragging = false;
        _advancedDragStartIndex = -1;
        _advancedDragMinIndex = -1;
        _advancedDragMaxIndex = -1;
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

    private async void ThumbnailCard_DoubleTapped(object? sender, TappedEventArgs e)
    {
        if (IsPointerOnCheckbox(e.Source))
        {
            return;
        }

        if (sender is not Border border || border.DataContext is not VideoRow row)
        {
            return;
        }

        await CopyRowUrlAsync(row);
        e.Handled = true;
    }
}
