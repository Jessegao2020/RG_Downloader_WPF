using Microsoft.Extensions.DependencyInjection;
using RedgifsDownloader.ApplicationLayer;
using RedgifsDownloader.ApplicationLayer.Downloads;
using RedgifsDownloader.ApplicationLayer.Fikfap;
using RedgifsDownloader.ApplicationLayer.Interfaces;
using RedgifsDownloader.ApplicationLayer.Notifications;
using RedgifsDownloader.ApplicationLayer.Settings;
using RedgifsDownloader.ApplicationLayer.Reddit;
using RedgifsDownloader.ApplicationLayer.ImageSimilarity;
using RedgifsDownloader.Domain.Interfaces;
using RedgifsDownloader.Infrastructure;
using RedgifsDownloader.Infrastructure.Fikfap;
using RedgifsDownloader.Infrastructure.Redgifs;
using RedgifsDownloader.Infrastructure.Reddit;
using RedgifsDownloader.Avalonia.ViewModels;
using RedgifsDownloader.Infrastructure.Settings;
using RedgifsDownloader.Infrastructure.ImageSim;
using RedgifsDownloader.Avalonia.Services;

namespace RedgifsDownloader.Avalonia;

internal static class ServiceConfiguration
{
    public static ServiceProvider BuildProvider()
    {
        var services = new ServiceCollection();

        services.AddSingleton<VideoChangeNotifier>();
        services.AddSingleton<IDownloadAppService, DownloadAppService>();
        services.AddSingleton<IMediaCrawlerFactory, MediaCrawlerFactory>();
        services.AddSingleton<IVideoPathStrategy, VideoPathStrategy>();
        services.AddSingleton<IPlatformDownloadStrategy, PlatformDownloadStrategy>();

        services.AddHttpClient<HttpTransferDownloader>();
        services.AddSingleton<HttpTransferDownloader>();
        services.AddSingleton<FikfapM3u8Downloader>(_ =>
        {
            var http = new HttpClient();
            http.DefaultRequestHeaders.Remove("Accept-Encoding");
            return new FikfapM3u8Downloader(http);
        });

        services.AddSingleton<RedgifsCrawler>();
        services.AddSingleton<FikfapCrawler>();

        services.AddSingleton<RedgifsAuthProvider>(_ =>
        {
            var http = new HttpClient();
            return new RedgifsAuthProvider(http);
        });
        services.AddSingleton<IPlatformAuthProvider>(sp => sp.GetRequiredService<RedgifsAuthProvider>());
        services.AddSingleton<RedgifsApiClient>(sp =>
        {
            var authProvider = sp.GetRequiredService<IPlatformAuthProvider>();
            return new RedgifsApiClient(new HttpClient(), authProvider);
        });

        services.AddSingleton<IFileStorage, FileStorage>();
        services.AddSingleton<IAppSettings, AvaloniaAppSettings>();
        services.AddSingleton<IUserNotificationService, NullUserNotificationService>();
        services.AddSingleton<ISecretProtector, PlainTextSecretProtector>();

        services.AddHttpClient<ThumbnailLoader>();
        services.AddSingleton<ThumbnailLoader>();

        services.AddSingleton<IFileNameStrategy, FileNameService>();
        services.AddSingleton<IFikfapApiClient, FikfapApiClient>();
        services.AddSingleton(new FikfapSession { Token = Guid.NewGuid().ToString() });

        services.AddSingleton<ITransferDownloader>(sp => sp.GetRequiredService<HttpTransferDownloader>());
        services.AddSingleton<IRedditApiClient, RedditApiClient>();
        services.AddHttpClient<IRedditAuthService, RedditAuthService>();
        services.AddSingleton<RedditFetchImagesAppService>();
        services.AddSingleton<RedditFetchRedgifsAppService>();
        services.AddSingleton<IRedditDownloadAppService, RedditDownloadAppService>();
        services.AddSingleton<IImageSimilarityAppService, ImageSimilarityAppService>();
        services.AddSingleton<IDupeFileMoveService, FileMoveService>();

        services.AddTransient<RedditViewModel>();
        services.AddTransient<ImageSimilarityViewModel>();

        return services.BuildServiceProvider();
    }
}

internal sealed class NullUserNotificationService : IUserNotificationService
{
    public void ShowMessage(string message) { }
}
