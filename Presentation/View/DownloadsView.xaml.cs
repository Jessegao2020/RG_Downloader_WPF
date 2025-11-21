using Microsoft.Extensions.DependencyInjection;
using RedgifsDownloader.ApplicationLayer.Settings;
using RedgifsDownloader.Model;
using RedgifsDownloader.Presentation.ViewModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;

namespace RedgifsDownloader.View
{
    public partial class DownloadsView : UserControl
    {
        private GridViewColumnHeader? _lastHeaderClicked;
        private ListSortDirection _lastDirection = ListSortDirection.Ascending;
        private DownloadsViewModel _downloadsvm => (DownloadsViewModel)DataContext;

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
            if(listView == null) return;

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

            // 切换选择状态
            vm.IsSelected = !vm.IsSelected;
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
        #endregion
    }
}
