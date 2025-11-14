using System.IO;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using RedgifsDownloader.Interfaces;
using RedgifsDownloader.Presentation.ViewModel;
using RedgifsDownloader.Services;
using RedgifsDownloader.Services.DupeCleaner;
using RedgifsDownloader.Services.Reddit;
using RedgifsDownloader.Services.RedGifs;

namespace RedgifsDownloader
{
    public partial class App : Application
    {
        private static readonly string LogDir = Path.Combine(AppContext.BaseDirectory, "logs");

        public static IServiceProvider ServiceProvider { get; private set; }

        public App()
        {
            GlobalExceptionHandler.Register();
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
            services.AddSingleton<RedditVideoDownloadCoordinator>();

            //底层
            services.AddSingleton<VideoFileService>();
            services.AddSingleton<DownloadWorker>();
            services.AddSingleton<ISettingsService, PropertySettingService>();
            services.AddHttpClient<RedditApiService>(client =>
            {
                client.DefaultRequestHeaders.UserAgent.ParseAdd("RedgifsDownloader/1.0 (by u/test_user)");
                client.DefaultRequestHeaders.Add("Cookie", "over18=1");
            });
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
    }
}
