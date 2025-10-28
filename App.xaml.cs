using Microsoft.Extensions.DependencyInjection;
using RedgifsDownloader.Interfaces;
using RedgifsDownloader.Services;
using RedgifsDownloader.Services.Reddit;
using RedgifsDownloader.ViewModel;
using System.Windows;

namespace RedgifsDownloader
{
    public partial class App : Application
    {
        public static IServiceProvider ServiceProvider { get; private set; }

        protected override void OnStartup(StartupEventArgs e)
        {
            var services = new ServiceCollection();

            // 注册依赖
            // 顶层
            services.AddSingleton<MainViewModel>(); 
            services.AddSingleton<DownloadsViewModel>();
            services.AddSingleton<SettingsViewModel>();
            services.AddSingleton<RedditViewModel>();

            // 中间层
            services.AddSingleton<ICrawlService, CrawlService>();
            services.AddSingleton<DownloadCoordinator>();

            //底层
            services.AddSingleton<VideoFileService>();
            services.AddSingleton<DownloadWorker>();
            services.AddSingleton<ISettingsService, PropertySettingService>();
            services.AddSingleton<IRedditApiService, RedditApiService>();
            services.AddSingleton<IRedditAuthService, RedditAuthService>();
            services.AddSingleton<RedditApiService>();

            

            //生成容器
            ServiceProvider = services.BuildServiceProvider();

            base.OnStartup(e);
        }
    }

}
