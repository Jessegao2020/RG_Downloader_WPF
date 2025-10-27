using Microsoft.Extensions.DependencyInjection;
using RedgifsDownloader.Model;
using RedgifsDownloader.ViewModel;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace RedgifsDownloader.View
{
    public partial class DownloadsView : UserControl
    {
        private DownloadsViewModel _downloadsvm => (DownloadsViewModel)DataContext;

        public DownloadsView()
        {
            InitializeComponent();

            DataContext = App.ServiceProvider.GetRequiredService<DownloadsViewModel>();
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

        private void UserBox_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                _downloadsvm.CrawlCommand.Execute(null);
            }
        }

        #endregion
    }
}
