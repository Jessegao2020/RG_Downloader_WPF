using Microsoft.Extensions.DependencyInjection;
using RedgifsDownloader.Interfaces;
using RedgifsDownloader.Services;
using RedgifsDownloader.Services.DupeCleaner;
using RedgifsDownloader.Services.Reddit;
using RedgifsDownloader.Services.RedGifs;
using RedgifsDownloader.ViewModel;
using System.IO;
using System.Text;
using System.Windows;

namespace RedgifsDownloader
{
    public partial class App : Application
    {
        private static readonly string LogDir = Path.Combine(AppContext.BaseDirectory, "logs");

        public static IServiceProvider ServiceProvider { get; private set; }

        public App()
        {
            this.DispatcherUnhandledException += (s, e) =>
            {
                LogException("DispatcherUnhandledException", e.Exception);
                MessageBox.Show("程序崩溃，已保存日志，请查看 logs 文件夹。");
                // 不设置 e.Handled = true，让程序自然崩溃
            };

            TaskScheduler.UnobservedTaskException += (s, e) =>
            {
                LogException("UnobservedTaskException", e.Exception);
                throw e.Exception; // 崩溃
            };

            AppDomain.CurrentDomain.UnhandledException += (s, e) =>
            {
                LogException("UnhandledException", e.ExceptionObject as Exception);
                Environment.FailFast("UnhandledException", e.ExceptionObject as Exception);
            };
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            var services = new ServiceCollection();

            // 注册依赖
            // 顶层
            services.AddSingleton<MainViewModel>(); 
            services.AddSingleton<DownloadsViewModel>();
            services.AddSingleton<SettingsViewModel>();
            services.AddSingleton<RedditViewModel>();
            services.AddSingleton<ImageSimilarityViewModel>();
            services.AddSingleton<DupePicsCleanerViewModel>();

            // 中间层
            services.AddSingleton<ICrawlService, CrawlService>();
            services.AddSingleton<DownloadCoordinator>();
            services.AddSingleton<RedditDownloadCoordinator>();

            //底层
            services.AddSingleton<VideoFileService>();
            services.AddSingleton<DownloadWorker>();
            services.AddSingleton<ISettingsService, PropertySettingService>();
            services.AddSingleton<IRedditApiService, RedditApiService>();
            services.AddSingleton<IRedditAuthService, RedditAuthService>();
            services.AddSingleton<ILogService, LogService>();
            services.AddSingleton<DupeCleanerService>();
            services.AddSingleton<RenameService>();
            services.AddSingleton<RedgifsAuthService>();
            services.AddHttpClient<RedditImageDownloadService>(client =>
            {
                client.DefaultRequestHeaders.UserAgent.ParseAdd("RedgifsDownloader/1.0 (by u/test_user)");
                client.DefaultRequestHeaders.Add("Cookie", "over18=1");
            });

            //生成容器
            ServiceProvider = services.BuildServiceProvider();

            base.OnStartup(e);
        }

        private static void LogException(string type, Exception? ex)
        {
            try
            {
                Directory.CreateDirectory(LogDir);
                string logFile = Path.Combine(LogDir, $"crash_{DateTime.Now:yyyyMMdd_HHmmss_fff}.txt"); // 加上毫秒级时间戳
                var sb = new StringBuilder();
                sb.AppendLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {type}");
                if (ex != null)
                {
                    sb.AppendLine($"Type: {ex.GetType().FullName}");
                    sb.AppendLine($"Message: {ex.Message}");
                    sb.AppendLine($"StackTrace:\n{ex.StackTrace}");
                }
                else sb.AppendLine("Exception was null");

                sb.AppendLine(new string('-', 80));
                File.AppendAllText(logFile, sb.ToString(), Encoding.UTF8);
            }
            catch { }
        }
    }

}
