using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.Extensions.DependencyInjection;
using RedgifsDownloader.ApplicationLayer.Settings;
using RedgifsDownloader.Presentation.ViewModel;

namespace RedgifsDownloader.View
{
    public partial class DownloadsView : UserControl
    {
        private GridViewColumnHeader? _lastHeaderClicked;
        private ListSortDirection _lastDirection = ListSortDirection.Ascending;

        private DownloadsViewModel _downloadsvm => (DownloadsViewModel)DataContext;

        // 拖动选择相关
        private bool _isDragging = false;
        private int _dragStartIndex = -1;
        private int _dragMaxIndex = -1; // 记录拖拽过程中的最大索引
        private int _dragMinIndex = -1; // 记录拖拽过程中的最小索引
        private Point _dragStartPoint;

        // Shift范围选择相关
        private int _lastClickedIndex = -1; // 记录最后点击的索引（用于Shift范围选择）
        private bool _isShiftSession = false; // 是否在Shift会话中
        private int _shiftStartIndex = -1; // Shift会话的起点
        private int _shiftMaxIndex = -1; // Shift会话接触过的最大索引
        private int _shiftMinIndex = -1; // Shift会话接触过的最小索引

        public int ThumbnailColumns
        {
            get => (int)GetValue(ThumbnailColumnsProperty);
            set => SetValue(ThumbnailColumnsProperty, value);
        }

        public static readonly DependencyProperty ThumbnailColumnsProperty = DependencyProperty.Register("ThumbnailColumns", typeof(int), typeof(DownloadsView), new PropertyMetadata(5));

        public DownloadsView()
        {
            InitializeComponent();

            DataContext = App.ServiceProvider.GetRequiredService<DownloadsViewModel>();
        }

        #region Misc UI Handlers (暂时不用重构)

        private void ListView_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (sender is not ListView lv) return;

            if (lv.SelectedItem is VideoViewModel vm && !string.IsNullOrEmpty(vm.Url))
            {
                Clipboard.SetText(vm.Url);

                // 根据 Tag 区分 toast 或 messagebox（你喜欢可以改成其它方式）
                if (lv.Tag as string == "Active")
                    ToastWindow.Show($"已复制 URL:\n{vm.Url}");
                else
                    ToastWindow.Show($"已复制 URL:\n{vm.Url}");
            }
        }

        private void GridViewColumnHeader_Click(object sender, RoutedEventArgs e)
        {
            if (e.OriginalSource is not GridViewColumnHeader header) return;

            var listView = sender as ListView;
            if (listView == null) return;

            // 优先用 Tag 指定的排序字段；否则退回到绑定路径
            string? sortBy = header.Tag as string;
            if (sortBy is null && header.Column?.DisplayMemberBinding is Binding b)
                sortBy = b.Path.Path;

            if (string.IsNullOrEmpty(sortBy)) return;

            var dir = (_lastHeaderClicked == header && _lastDirection == ListSortDirection.Ascending)
                ? ListSortDirection.Descending
                : ListSortDirection.Ascending;

            var view = CollectionViewSource.GetDefaultView(listView.ItemsSource);
            view.SortDescriptions.Clear();
            view.SortDescriptions.Add(new SortDescription(sortBy, dir));
            view.Refresh();

            _lastHeaderClicked = header;
            _lastDirection = dir;
        }

        private void BtnOpenFolder_Click(object sender, RoutedEventArgs e)
        {
            var settings = App.ServiceProvider.GetRequiredService<IAppSettings>();
            string folderPath = settings.DownloadDirectory;

            if (Directory.Exists(folderPath))
                Process.Start(new ProcessStartInfo()
                {
                    FileName = folderPath,
                    UseShellExecute = true
                });
            else
                MessageBox.Show("文件夹不存在。", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        private void UserBox_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                _downloadsvm.CrawlCommand.Execute(null);
            }
        }

        private void ListView_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (sender is not ListView lv) return;
            if (lv.View is not GridView gv) return;

            double total = lv.ActualWidth - 35;

            switch (lv.Tag)
            {
                case "Active":
                    gv.Columns[0].Width = 30;
                    gv.Columns[1].Width = total * 0.45;
                    gv.Columns[2].Width = total * 0.25;
                    gv.Columns[3].Width = total * 0.20;
                    break;

                case "Failed":
                    gv.Columns[0].Width = total * 0.80;
                    gv.Columns[1].Width = total * 0.20;
                    break;
            }
        }

        private void Thumbnail_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is not Border border) return;
            if (border.DataContext is not VideoViewModel vm) return;

            // 如果点击的是CheckBox，不处理（让CheckBox自己处理）
            if (e.OriginalSource is CheckBox)
                return;

            // 获取当前项的索引
            int currentIndex = _downloadsvm.Videos.IndexOf(vm);
            if (currentIndex < 0) return;

            // 检查是否按下Shift键
            bool isShiftPressed = Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift);

            if (isShiftPressed)
            {
                // Shift模式：执行范围选择
                if (!_isShiftSession)
                {
                    // 开始新的Shift会话
                    _isShiftSession = true;
                    _shiftStartIndex = _lastClickedIndex >= 0 ? _lastClickedIndex : currentIndex;
                    _shiftMinIndex = Math.Min(_shiftStartIndex, currentIndex);
                    _shiftMaxIndex = Math.Max(_shiftStartIndex, currentIndex);
                    _downloadsvm.BeginRangeSelect();
                }
                else
                {
                    // 继续Shift会话，更新接触范围
                    _shiftMinIndex = Math.Min(_shiftMinIndex, Math.Min(_shiftStartIndex, currentIndex));
                    _shiftMaxIndex = Math.Max(_shiftMaxIndex, Math.Max(_shiftStartIndex, currentIndex));
                }

                // 执行范围选择
                _downloadsvm.RangeSelect(_shiftStartIndex, currentIndex, _shiftMinIndex, _shiftMaxIndex);
                _lastClickedIndex = currentIndex;
                _dragStartIndex = -1; // 不启用拖动
            }
            else
            {
                // 松开Shift，结束Shift会话
                if (_isShiftSession)
                {
                    _downloadsvm.EndRangeSelect();
                    _isShiftSession = false;
                    _shiftStartIndex = -1;
                    _shiftMinIndex = -1;
                    _shiftMaxIndex = -1;
                }

                // 普通点击：不立即改变状态，等待判断是点击还是拖拽
                _dragStartIndex = currentIndex;
                _dragStartPoint = e.GetPosition(ThumbnailScrollViewer);
                _isDragging = false;
            }

            e.Handled = true;
        }

        private void Thumbnail_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (sender is not Border border) return;
            if (border.DataContext is not VideoViewModel vm) return;

            if (!string.IsNullOrEmpty(vm.Url))
            {
                Clipboard.SetText(vm.Url);
                ToastWindow.Show($"已复制 URL:\n{vm.Url}");
            }
        }

        private void ScrollViewer_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            // 检查Shift状态，如果松开了Shift且在Shift会话中，结束会话
            bool isShiftPressed = Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift);
            if (_isShiftSession && !isShiftPressed)
            {
                _downloadsvm.EndRangeSelect();
                _isShiftSession = false;
                _shiftStartIndex = -1;
                _shiftMinIndex = -1;
                _shiftMaxIndex = -1;
            }

            // 检查鼠标左键是否按下
            if (e.LeftButton != MouseButtonState.Pressed)
            {
                if (_isDragging)
                    _downloadsvm.EndRangeSelect();

                _isDragging = false;
                _dragStartIndex = -1;
                _dragMinIndex = -1;
                _dragMaxIndex = -1;
                return;
            }

            // 如果还没开始拖动，检查是否移动超过系统阈值
            if (!_isDragging && _dragStartIndex >= 0)
            {
                var currentPoint = e.GetPosition(ThumbnailScrollViewer);
                var deltaX = Math.Abs(currentPoint.X - _dragStartPoint.X);
                var deltaY = Math.Abs(currentPoint.Y - _dragStartPoint.Y);

                // 使用系统定义的拖动阈值
                if (deltaX > SystemParameters.MinimumHorizontalDragDistance ||
                    deltaY > SystemParameters.MinimumVerticalDragDistance)
                {
                    _isDragging = true;
                    _dragMinIndex = _dragStartIndex;
                    _dragMaxIndex = _dragStartIndex;
                    _downloadsvm.BeginRangeSelect();
                }
                else
                {
                    return; // 未达到阈值，不处理
                }
            }

            if (!_isDragging) return;

            // 获取鼠标位置并查找下方的元素
            var position = e.GetPosition(ThumbnailItemsControl);
            var hitResult = VisualTreeHelper.HitTest(ThumbnailItemsControl, position);

            if (hitResult != null)
            {
                var element = hitResult.VisualHit;
                while (element != null && element != ThumbnailItemsControl)
                {
                    if (element is Border border && border.DataContext is VideoViewModel vm)
                    {
                        int currentIndex = _downloadsvm.Videos.IndexOf(vm);
                        if (currentIndex >= 0 && _dragStartIndex >= 0)
                        {
                            // 更新拖拽接触过的最大范围
                            _dragMinIndex = Math.Min(_dragMinIndex, Math.Min(_dragStartIndex, currentIndex));
                            _dragMaxIndex = Math.Max(_dragMaxIndex, Math.Max(_dragStartIndex, currentIndex));

                            _downloadsvm.RangeSelect(_dragStartIndex, currentIndex, _dragMinIndex, _dragMaxIndex);
                        }
                        break;
                    }
                    element = VisualTreeHelper.GetParent(element);
                }
            }
        }

        private void ScrollViewer_Thumbnail_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (sender is not ScrollViewer sv)
                return;

            double targetItemWidth = 120.0;
            double availableWidth = e.NewSize.Width;

            int columns = (int)(availableWidth / targetItemWidth);

            if (columns < 1) columns = 1;

            ThumbnailColumns = columns;
        }

        private void ScrollViewer_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (_isDragging)
            {
                _downloadsvm.EndRangeSelect();
            }
            else if (_dragStartIndex >= 0 && _dragStartIndex < _downloadsvm.Videos.Count)
            {
                // 纯点击（没有发生拖拽）：切换选中状态
                _downloadsvm.Videos[_dragStartIndex].IsSelected = !_downloadsvm.Videos[_dragStartIndex].IsSelected;
                _lastClickedIndex = _dragStartIndex;
            }

            // 检查是否松开了Shift键，结束Shift会话
            if (_isShiftSession && !(Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift)))
            {
                _downloadsvm.EndRangeSelect();
                _isShiftSession = false;
                _shiftStartIndex = -1;
                _shiftMinIndex = -1;
                _shiftMaxIndex = -1;
            }

            _isDragging = false;
            _dragStartIndex = -1;
            _dragMinIndex = -1;
            _dragMaxIndex = -1;
        }

        private void ScrollViewer_MouseLeave(object sender, MouseEventArgs e)
        {
            if (_isDragging)
                _downloadsvm.EndRangeSelect();

            _isDragging = false;
            _dragStartIndex = -1;
            _dragMinIndex = -1;
            _dragMaxIndex = -1;
        }
        #endregion
    }
}
