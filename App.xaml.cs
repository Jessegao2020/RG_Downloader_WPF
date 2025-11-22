using System.IO;
using System.Net.Http;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using RedgifsDownloader.ApplicationLayer;
using RedgifsDownloader.ApplicationLayer.Downloads;
using RedgifsDownloader.ApplicationLayer.DupeCleaner;
using RedgifsDownloader.ApplicationLayer.Fikfap;
using RedgifsDownloader.ApplicationLayer.ImageSimilarity;
using RedgifsDownloader.ApplicationLayer.Interfaces;
using RedgifsDownloader.ApplicationLayer.Notifications;
using RedgifsDownloader.ApplicationLayer.Reddit;
using RedgifsDownloader.ApplicationLayer.Settings;
using RedgifsDownloader.Domain.Interfaces;
using RedgifsDownloader.Infrastructure;
using RedgifsDownloader.Infrastructure.DupeCleaner;
using RedgifsDownloader.Infrastructure.Fikfap;
using RedgifsDownloader.Infrastructure.ImageSim;
using RedgifsDownloader.Infrastructure.Reddit;
using RedgifsDownloader.Infrastructure.Redgifs;
using RedgifsDownloader.Infrastructure.Settings;
using RedgifsDownloader.Presentation.ViewModel;

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
            services.AddSingleton<VideoChangeNotifier>(); // 视频变化通知服务
            services.AddSingleton<IRedditDownloadAppService, RedditDownloadAppService>();
            services.AddSingleton<RedditFetchImagesAppService>();
            services.AddSingleton<RedditFetchRedgifsAppService>();
            services.AddSingleton<IDownloadAppService, DownloadAppService>();
            services.AddSingleton<IMediaCrawlerFactory, MediaCrawlerFactory>();
            services.AddSingleton<IVideoPathStrategy, VideoPathStrategy>();
            services.AddSingleton<IPlatformDownloadStrategy, PlatformDownloadStrategy>();
            services.AddSingleton<IFileStorage, FileStorage>();
            services.AddSingleton<RedgifsApiClient>(); // Redgifs API 客户端
            services.AddSingleton<RedgifsCrawler>();
            services.AddSingleton<FikfapCrawler>();
            services.AddSingleton<IImageSimilarityAppService, ImageSimilarityAppService>();
            services.AddSingleton<IDupeFileMoveService, FileMoveService>();
            services.AddSingleton<DupeCleanerAppService>();
            services.AddSingleton(new FikfapSession { Token = Guid.NewGuid().ToString() });

            //底层
            services.AddSingleton<IRedditApiClient, RedditApiClient>();
            services.AddSingleton<IUserNotificationService, LogService>();
            services.AddSingleton<IAppSettings, AppSettings>();
            services.AddSingleton<IMediaCrawler, RedgifsCrawler>();

            // Redgifs 相关服务
            services.AddSingleton<RedgifsAuthProvider>(sp => new RedgifsAuthProvider(new HttpClient()));
            services.AddSingleton<IPlatformAuthProvider>(sp => sp.GetRequiredService<RedgifsAuthProvider>());
            services.AddSingleton<RedgifsApiClient>(sp =>
            {
                var authProvider = sp.GetRequiredService<IPlatformAuthProvider>();
                return new RedgifsApiClient(new HttpClient(), authProvider);
            });

            services.AddSingleton<IImageHashService, ImageHashService>();
            services.AddSingleton<IDupeCleanerService, DupeCleanerService>();
            services.AddHttpClient<IRedditAuthService, RedditAuthService>();
            services.AddSingleton<IRenameService, DupeRenameService>();
            services.AddSingleton<IFileNameStrategy, FileNameService>();
            services.AddSingleton<IFikfapApiClient, FikfapApiClient>();
            services.AddSingleton<YtDlpTransferDownloader>();
            services.AddHttpClient<HttpTransferDownloader>();
            services.AddSingleton<HttpTransferDownloader>();
            services.AddSingleton<ITransferDownloader>(sp => sp.GetRequiredService<HttpTransferDownloader>());
            //生成容器
            ServiceProvider = services.BuildServiceProvider();

            base.OnStartup(e);
        }
    }
}
