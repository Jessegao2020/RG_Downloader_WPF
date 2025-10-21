using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Text.Json;
using System.Windows;
using System.IO;
using System.ComponentModel;
using System.Net.Http;
using System.Windows.Controls;

namespace RedgifsDownloader
{

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        #region Fields & Properties

        public ObservableCollection<VideoItem> Videos { get; } = new();
        public ObservableCollection<VideoItem> Failed { get; } = new();

        private static readonly HttpClient httpClient = new HttpClient();
        private CancellationTokenSource? _cts;
        private bool _isDownloading = false;

        private int _videosCount;
        public int VideosCount
        {
            get => _videosCount;
            set
            {
                _videosCount = value;
                // 保证在 UI 线程触发 PropertyChanged（与原行为一致）
                Application.Current.Dispatcher.Invoke(() => OnPropertyChanged(nameof(VideosCount)));
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        #endregion

        #region ctor & Window lifecycle

        public MainWindow()
        {
            InitializeComponent();

            // 绑定集合到 ListView（同原）
            ListViewResults.ItemsSource = Videos;
            ListViewFailed.ItemsSource = Failed;
            DataContext = this;

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

        #region Crawl (BtnCrawl) — 拆分后逻辑清晰

        private async void BtnCrawl_Click(object sender, RoutedEventArgs e)
        {            
            try
            {
                var user = GetTrimmedUserInput();
                if (string.IsNullOrEmpty(user))
                {
                    MessageBox.Show("请输入 User 名");
                    return;
                }

                PrepareForCrawlUi();

                await StartCrawlAsync(user);
            }
            finally
            {
                RestoreCrawlUi();
            }
        }

        private string GetTrimmedUserInput() => UserBox?.Text?.Trim() ?? string.Empty;

        private void PrepareForCrawlUi()
        {
            BtnCrawl.IsEnabled = false;
            BtnCrawl.Content = "Crawling...";
            Videos.Clear();
            Failed.Clear();
            VideosCount = 0;
        }

        private void RestoreCrawlUi()
        {
            BtnCrawl.IsEnabled = true;
            BtnCrawl.Content = "Crawl";
            if (VideosCount > 0)
            {
                MessageBox.Show($"{VideosCount} videos found.", "Result");
            }
        }

        private async Task StartCrawlAsync(string user)
        {
            string appBaseDir = AppContext.BaseDirectory;
            string spiderPath = Path.Combine(appBaseDir, "videoSpider");
            string outputFile = Path.Combine(spiderPath, "videos.json");
            if (File.Exists(outputFile)) File.Delete(outputFile);

            var psi = new ProcessStartInfo
            {
                FileName = "python",
                WorkingDirectory = spiderPath,
                Arguments = $"-u -m scrapy crawl videos -a user={user} --loglevel ERROR",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            await RunSpiderProcessAsync(psi);
        }

        private async Task RunSpiderProcessAsync(ProcessStartInfo psi)
        {


            var process = Process.Start(psi) ?? throw new InvalidOperationException("无法启动爬虫进程");

            process.OutputDataReceived += (s, e) =>
            {
                if (string.IsNullOrEmpty(e.Data)) return;
                try
                {
                    var video = JsonSerializer.Deserialize<VideoItem>(e.Data);
                    if (video != null)
                    {
                        Application.Current.Dispatcher.BeginInvoke(() =>
                        {
                            Videos.Add(video);
                            VideosCount = Videos.Count;
                        });
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"JSON parse error: {ex.Message}: {e.Data}");
                }
            };

            process.ErrorDataReceived += (s, e) =>
            {
                if (string.IsNullOrEmpty(e.Data)) return;
                Debug.WriteLine(e.Data);
                if (e.Data.StartsWith("ERROR_MSG:"))
                {
                    string msg = e.Data.Substring("ERROR_MSG:".Length).Trim();
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        MessageBox.Show(msg, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    });
                }
            };

            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            await process.WaitForExitAsync();
            process.WaitForExit();
        }

        #endregion

        #region Download (BtnDownload) — 拆分并保持行为

        private async void BtnDownload_Click(object sender, RoutedEventArgs e)
        {            
            await StartOrStopDownloadAsync();
        }

        private async Task StartOrStopDownloadAsync()
        {
            _isDownloading = true;
            BtnDownload.Content = "下载中..";
            BtnDownload.IsEnabled = false;

            string baseDir = EnsureDownloadBaseDirectory();

            int maxConcurrency = GetMaxConcurrencyFromUi();

            using var semaphore = new SemaphoreSlim(maxConcurrency);
            _cts = new CancellationTokenSource();

            InitializeVideoStatusesFromDisk(baseDir);
            UpdateStatusCounters(); // 更新计数器（初始）

            var tasks = Videos.Select(video => DownloadWorkerAsync(video, baseDir, semaphore, _cts.Token)).ToArray();

            try
            {
                await Task.WhenAll(tasks);
            }
            catch (OperationCanceledException)
            {
                // 用户主动停止，不弹窗
            }
            finally
            {
                _isDownloading = false;
                BtnDownload.Content = "下载";
                BtnDownload.IsEnabled = true;
                MessageBox.Show($"任务结束。成功 {Videos.Count(v => v.status == "完成" || v.status == "已存在")}，失败 {Failed.Count}。");
            }
        }

        private string EnsureDownloadBaseDirectory()
        {
            string appBaseDir = AppContext.BaseDirectory;
            string baseDir = Path.Combine(appBaseDir, "Downloads");
            Directory.CreateDirectory(baseDir);
            return baseDir;
        }

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

        private void InitializeVideoStatusesFromDisk(string baseDir)
        {
            foreach (var video in Videos)
            {
                if (string.IsNullOrEmpty(video.url)) continue;

                string userName = string.IsNullOrWhiteSpace(video.userName) ? "Unknown" : video.userName.Trim();
                string saveDir = Path.Combine(baseDir, userName);
                Directory.CreateDirectory(saveDir);

                string path = Path.Combine(saveDir, video.id + ".mp4");

                if (File.Exists(path))
                {
                    video.status = "已存在";
                    // 保持原逻辑：如果已存在则从 Failed 中移除
                    if (Failed.Contains(video)) Application.Current.Dispatcher.Invoke(() => Failed.Remove(video));
                }
                else if (video.status == "已存在" || video.status == "完成")
                {
                    video.status = null;
                }
            }
        }

        private async Task DownloadWorkerAsync(VideoItem video, string baseDir, SemaphoreSlim semaphore, CancellationToken token)
        {
            await semaphore.WaitAsync(token);
            try
            {
                await DownloadSingleVideoAsync(video, baseDir, token);
            }
            catch (OperationCanceledException)
            {
                HandleCancellation(video);
            }
            catch (Exception)
            {
                HandleFailure(video);
            }
            finally
            {
                // 在线程结束（无论如何）更新统计并释放信号量
                Application.Current.Dispatcher.Invoke(UpdateStatusCounters);
                semaphore.Release();
            }
        }

        private async Task DownloadSingleVideoAsync(VideoItem video, string baseDir, CancellationToken token)
        {
            if (token.IsCancellationRequested) token.ThrowIfCancellationRequested();

            if (string.IsNullOrEmpty(video.url)) return;

            string userName = string.IsNullOrWhiteSpace(video.userName) ? "Unknown" : video.userName.Trim();
            string saveDir = Path.Combine(baseDir, userName);
            Directory.CreateDirectory(saveDir);

            string path = Path.Combine(saveDir, video.id + ".mp4");

            if (File.Exists(path))
            {
                video.status = "已存在";
                video.progress = 100;
                return;
            }

            using var request = new HttpRequestMessage(HttpMethod.Get, video.url);
            request.Headers.Add("User-Agent", "Mozilla/5.0");
            request.Headers.Add("Referer", "https://www.redgifs.com/");
            request.Headers.Add("Authorization", "Bearer " + video.token);
            request.Headers.Add("Accept", "application/json, text/plain, */*");

            using var response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, token);
            response.EnsureSuccessStatusCode();

            var totalBytes = response.Content.Headers.ContentLength ?? -1L;
            bool canReport = totalBytes > 0;

            using var stream = await response.Content.ReadAsStreamAsync(token);
            using var fs = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None);

            byte[] buffer = new byte[8192];
            long totalRead = 0;
            int read;
            video.status = "0%";
            video.progress = 0;

            // 为避免 UI 过于频繁更新，这里只在进度变化 >= 1% 时更新（可改）
            double lastReportedPercent = 0;

            while ((read = await stream.ReadAsync(buffer, 0, buffer.Length, token)) > 0)
            {
                await fs.WriteAsync(buffer, 0, read, token);
                totalRead += read;

                if (canReport)
                {
                    double percent = Math.Round((double)totalRead / totalBytes * 100, 1);
                    if (percent - lastReportedPercent >= 1.0 || percent == 100.0) // 阈值刷新
                    {
                        lastReportedPercent = percent;
                        // 只更新绑定属性，界面通过 INotifyPropertyChanged 自动刷新
                        video.status = $"{percent}%";
                        video.progress = percent;
                    }
                }
            }

            video.status = "完成";
            video.progress = 100;
        }

        private void HandleCancellation(VideoItem video)
        {
            // 保留原行为：取消时标记为已停止，但不自动加入 Failed 集合
            if (video.status != "完成" && video.status != "已存在")
                video.status = "已停止";
        }

        private void HandleFailure(VideoItem video)
        {
            video.status = "失败";
            Application.Current.Dispatcher.Invoke(() =>
            {
                if (!Failed.Contains(video))
                    Failed.Add(video);
            });
        }

        #endregion

        #region Utilities / UI helpers

        private void UpdateStatusCounters()
        {
            int total = Videos.Count;
            TxtDownloadedCount.Text = $"{Videos.Count(v => v.status == "完成" || v.status == "已存在")}/{total}";
            TxtFailedCount.Text = Failed.Count.ToString();
        }

        #endregion

        #region Misc UI Handlers (双击复制 etc)

        private void ListViewResults_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (ListViewResults.SelectedItem is VideoItem video && !string.IsNullOrEmpty(video.url))
            {
                Clipboard.SetText(video.url);
                MessageBox.Show($"已复制 URL:\n{video.url}", "复制成功", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void ListViewFailed_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (ListViewFailed.SelectedItem is VideoItem video && !string.IsNullOrEmpty(video.url))
            {
                Clipboard.SetText(video.url);
                MessageBox.Show($"已复制 URL:\n{video.url}", "复制成功", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        // 预留按钮处理器（如果你之后要实现停止 / 重试全部）
        private void BtnStop_Click(object sender, RoutedEventArgs e)
        {
            if (_isDownloading)
            {
                _cts?.Cancel();
                BtnDownload.Content = "下载";
                BtnDownload.IsEnabled = true;
                _isDownloading = false;
                return;
            }            
        }

        private void BtnRetryAll_Click(object sender, RoutedEventArgs e)
        {
            // 预留：将 Failed 项移回 Videos 并重置状态，然后触发下载
            // 目前保留空实现，和原行为一致
        }

        #endregion
    }
}