using System.IO;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using RedgifsDownloader.ApplicationLayer;
using RedgifsDownloader.ApplicationLayer.Downloads;
using RedgifsDownloader.ApplicationLayer.Interfaces;
using RedgifsDownloader.ApplicationLayer.Reddit;
using RedgifsDownloader.ApplicationLayer.Settings;
using RedgifsDownloader.Domain.Interfaces;
using RedgifsDownloader.Infrastructure;
using RedgifsDownloader.Infrastructure.Reddit;
using RedgifsDownloader.Infrastructure.Redgifs;
using RedgifsDownloader.Infrastructure.Settings;
using RedgifsDownloader.Interfaces;
using RedgifsDownloader.Presentation.ViewModel;
using RedgifsDownloader.Services;
using RedgifsDownloader.Services.DupeCleaner;

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
            services.AddSingleton<IRedditDownloadAppService, RedditDownloadAppService>();
            services.AddSingleton<RedditFetchImagesAppService>();
            services.AddSingleton<RedditFetchRedgifsAppService>();
            services.AddSingleton<IDownloadAppService, DownloadAppService>();
            services.AddSingleton<IMediaCrawlerFactory, MediaCrawlerFactory>();
            services.AddHttpClient<IMediaDownloader, HttpMediaDownloader>();
            services.AddSingleton<IVideoPathStrategy, VideoPathStrategy>();
            services.AddSingleton<IFileStorage, FileStorage>();
            services.AddSingleton<RedgifsCrawler>();


            //底层
            services.AddSingleton<IRedditApiClient, RedditApiClient>();
            services.AddSingleton<ILogService, LogService>();
            services.AddSingleton<DupeCleanerService>();
            services.AddSingleton<RenameService>();
            services.AddSingleton<IAppSettings, AppSettings>();
            services.AddSingleton<IMediaCrawler, RedgifsCrawler>();
            services.AddHttpClient<IRedditAuthService, RedditAuthService>();
            //生成容器
            ServiceProvider = services.BuildServiceProvider();

            base.OnStartup(e);
        }
    }
}
