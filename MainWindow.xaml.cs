using RedgifsDownloader.Services;
using RedgifsDownloader.ViewModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Windows;
using System.Windows.Controls;

namespace RedgifsDownloader
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly MainViewModel _vm;

        #region Fields & Properties

        private static readonly HttpClient httpClient = new HttpClient();

        #endregion

        #region ctor & Window lifecycle

        public MainWindow()
        {
            InitializeComponent();

            var fileService = new VideoFileService();
            var worker = new DownloadWorker();
            var coordinator = new DownloadCoordinator(worker, fileService);
            var crawler = new CrawlService();
            _vm = new MainViewModel(coordinator, crawler);

            // 绑定集合到 ListView（同原）
            ListViewResults.ItemsSource = _vm.Videos;
            ListViewFailed.ItemsSource = _vm.Failed;
            DataContext = _vm;

            // 恢复上次设置（最大并发、窗口位置）
            RestoreMaxConcurrencyFromSettings();
            RestoreWindowPosition();
        }

        private void RestoreMaxConcurrencyFromSettings()
        {
            int savedMax = Properties.Settings.Default.MaxDownloadCount;
            if (savedMax <= 0 || savedMax > 50) savedMax = 5;

            if (maxDownloadComboBox != null)
            {
                var matchItem = maxDownloadComboBox.Items
                    .OfType<ComboBoxItem>()
                    .FirstOrDefault(i => i.Content.ToString() == savedMax.ToString());

                if (matchItem != null)
                    maxDownloadComboBox.SelectedItem = matchItem;
                else
                    maxDownloadComboBox.Text = savedMax.ToString();
            }
        }

        private void RestoreWindowPosition()
        {
            this.Top = Properties.Settings.Default.WindowTop;
            this.Left = Properties.Settings.Default.WindowLeft;
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            SaveMaxConcurrencyToSettings();
            SaveWindowPositionToSettings();
            Properties.Settings.Default.Save();
        }

        private void SaveMaxConcurrencyToSettings()
        {
            string text;
            if (maxDownloadComboBox.SelectedItem is ComboBoxItem item)
                text = item.Content.ToString();
            else
                text = maxDownloadComboBox.Text;

            if (int.TryParse(text, out int parsed) && parsed > 0 && parsed <= 50)
                Properties.Settings.Default.MaxDownloadCount = parsed;
            else
                Properties.Settings.Default.MaxDownloadCount = 5;
        }

        private void SaveWindowPositionToSettings()
        {
            if (this.WindowState == WindowState.Normal)
            {
                Properties.Settings.Default.WindowTop = this.Top;
                Properties.Settings.Default.WindowLeft = this.Left;
            }
        }

        #endregion

        #region Download (BtnDownload) — 拆分并保持行为
        
        // 暂时不重构，后期可用Command控制
        private int GetMaxConcurrencyFromUi()
        {
            int maxConcurrency = 5;
            if (maxDownloadComboBox.SelectedItem is ComboBoxItem selectedItem &&
                int.TryParse(selectedItem.Content.ToString(), out int parsed))
            {
                maxConcurrency = parsed;
            }
            return maxConcurrency;
        }
        #endregion

        #region Misc UI Handlers (暂时不用重构)

        private void ListViewResults_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (ListViewResults.SelectedItem is VideoItem video && !string.IsNullOrEmpty(video.Url))
            {
                Clipboard.SetText(video.Url);
                MessageBox.Show($"已复制 URL:\n{video.Url}", "复制成功", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void ListViewFailed_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (ListViewFailed.SelectedItem is VideoItem video && !string.IsNullOrEmpty(video.Url))
            {
                Clipboard.SetText(video.Url);
                MessageBox.Show($"已复制 URL:\n{video.Url}", "复制成功", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        // 预留按钮处理器（如果你之后要实现停止 / 重试全部）
        

        private void BtnRetryAll_Click(object sender, RoutedEventArgs e)
        {
            // 预留：将 Failed 项移回 Videos 并重置状态，然后触发下载
            // 目前保留空实现，和原行为一致
        }

        private void BtnOpenFolder_Click(object sender, RoutedEventArgs e)
        {
            string folderPath = System.IO.Path.Combine(AppContext.BaseDirectory, "Downloads");

            if (Directory.Exists(folderPath))
                Process.Start(new ProcessStartInfo()
                {
                    FileName = folderPath,
                    UseShellExecute = true
                });
            else
                MessageBox.Show("文件夹不存在。", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        #endregion
    }
}