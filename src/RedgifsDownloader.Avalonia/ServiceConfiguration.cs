using Microsoft.Extensions.DependencyInjection;
using RedgifsDownloader.ApplicationLayer;
using RedgifsDownloader.ApplicationLayer.Downloads;
using RedgifsDownloader.ApplicationLayer.Fikfap;
using RedgifsDownloader.ApplicationLayer.Interfaces;
using RedgifsDownloader.ApplicationLayer.Notifications;
using RedgifsDownloader.ApplicationLayer.Settings;
using RedgifsDownloader.Domain.Interfaces;
using RedgifsDownloader.Infrastructure;
using RedgifsDownloader.Infrastructure.Fikfap;
using RedgifsDownloader.Infrastructure.Redgifs;
using RedgifsDownloader.Infrastructure.Settings;

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

        services.AddSingleton<IFileNameStrategy, FileNameService>();
        services.AddSingleton<IFikfapApiClient, FikfapApiClient>();
        services.AddSingleton(new FikfapSession { Token = Guid.NewGuid().ToString() });

        return services.BuildServiceProvider();
    }
}

internal sealed class NullUserNotificationService : IUserNotificationService
{
    public void ShowMessage(string message) { }
}
