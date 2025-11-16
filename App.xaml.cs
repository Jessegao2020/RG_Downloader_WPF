using System.IO;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using RedgifsDownloader.ApplicationLayer;
using RedgifsDownloader.ApplicationLayer.Downloads;
using RedgifsDownloader.ApplicationLayer.Interfaces;
using RedgifsDownloader.ApplicationLayer.Settings;
using RedgifsDownloader.Domain.Interfaces;
using RedgifsDownloader.Infrastructure;
using RedgifsDownloader.Infrastructure.Redgifs;
using RedgifsDownloader.Infrastructure.Settings;
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
            services.AddSingleton<DownloadsViewModelNew>();
            services.AddSingleton<SettingsViewModel>();
            services.AddSingleton<RedditViewModel>();
            services.AddSingleton<ImageSimilarityViewModel>();
            services.AddSingleton<DupePicsCleanerViewModel>();

            // 中间层
            services.AddSingleton<RedditDownloadCoordinator>();
            services.AddSingleton<RedditVideoDownloadCoordinator>();
            services.AddSingleton<IDownloadAppService, DownloadAppService>();
            services.AddSingleton<IMediaCrawlerFactory, MediaCrawlerFactory>();
            services.AddHttpClient<IMediaDownloader, HttpMediaDownloader>();
            services.AddSingleton<IVideoPathStrategy, VideoPathStrategy>();
            services.AddSingleton<IFileStorage, FileStorage>();
            services.AddSingleton<RedgifsCrawler>();


            //底层
            services.AddSingleton<VideoFileService>();
            services.AddSingleton<DownloadWorker>();
            services.AddSingleton<ISettingsService, PropertySettingService>();
            services.AddHttpClient<RedditApiService>(client =>
            {
                client.DefaultRequestHeaders.UserAgent.ParseAdd("RedgifsDownloader/1.0 (by u/test_user)");
                client.DefaultRequestHeaders.Add("Cookie", "over18=1");
            });
            services.AddSingleton<Interfaces.IRedditAuthService, Services.Reddit.RedditAuthService>();
            services.AddSingleton<ILogService, LogService>();
            services.AddSingleton<DupeCleanerService>();
            services.AddSingleton<RenameService>();
            services.AddSingleton<RedgifsAuthService>();
            services.AddHttpClient<RedditImageDownloadService>(client =>
            {
                client.DefaultRequestHeaders.UserAgent.ParseAdd("RedgifsDownloader/1.0 (by u/test_user)");
                client.DefaultRequestHeaders.Add("Cookie", "over18=1");
            });

            services.AddSingleton<IAppSettings, AppSettings>();
            services.AddSingleton<IMediaCrawler, RedgifsCrawler>();
            services.AddHttpClient<Domain.Interfaces.IRedditAuthService, Infrastructure.Reddit.RedditAuthService>();
            //生成容器
            ServiceProvider = services.BuildServiceProvider();

            base.OnStartup(e);
        }
    }
}
