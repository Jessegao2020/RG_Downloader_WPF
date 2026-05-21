using Microsoft.Extensions.DependencyInjection;
using RedgifsDownloader.ApplicationLayer;
using RedgifsDownloader.ApplicationLayer.Downloads;
using RedgifsDownloader.ApplicationLayer.Fikfap;
using RedgifsDownloader.ApplicationLayer.Interfaces;
using RedgifsDownloader.ApplicationLayer.Notifications;
using RedgifsDownloader.ApplicationLayer.Settings;
using RedgifsDownloader.Cli;
using RedgifsDownloader.Domain.Enums;
using RedgifsDownloader.Domain.Interfaces;
using RedgifsDownloader.Infrastructure;
using RedgifsDownloader.Infrastructure.Fikfap;
using RedgifsDownloader.Infrastructure.Redgifs;
using RedgifsDownloader.Infrastructure.Settings;

if (args.Length != 3 || !string.Equals(args[0], "crawl", StringComparison.OrdinalIgnoreCase) || !string.Equals(args[1], "redgifs", StringComparison.OrdinalIgnoreCase))
{
    PrintUsage();
    return 1;
}

var username = args[2];

var services = new ServiceCollection();
ConfigureServices(services);

using var provider = services.BuildServiceProvider();
var appService = provider.GetRequiredService<IDownloadAppService>();

await foreach (var video in appService.CrawlAsync(MediaPlatform.Redgifs, username))
{
    Console.WriteLine($"Id={video.Id}");
    Console.WriteLine($"Url={video.Url}");
    Console.WriteLine($"CreateDateRaw={video.CreateDateRaw}");
    Console.WriteLine($"HasThumbnailUrl={!string.IsNullOrWhiteSpace(video.ThumbnailUrl)}");
    Console.WriteLine();
}

return 0;

static void ConfigureServices(IServiceCollection services)
{
    services.AddSingleton<VideoChangeNotifier>();
    services.AddSingleton<IDownloadAppService, DownloadAppService>();
    services.AddSingleton<IMediaCrawlerFactory, MediaCrawlerFactory>();
    services.AddSingleton<IVideoPathStrategy, VideoPathStrategy>();
    services.AddSingleton<IPlatformDownloadStrategy, PlatformDownloadStrategy>();

    services.AddHttpClient<HttpTransferDownloader>();
    services.AddSingleton<HttpTransferDownloader>();
    services.AddSingleton<FikfapM3u8Downloader>(sp =>
    {
        var http = new HttpClient();
        http.DefaultRequestHeaders.Remove("Accept-Encoding");
        return new FikfapM3u8Downloader(http);
    });

    services.AddSingleton<RedgifsCrawler>();
    services.AddSingleton<FikfapCrawler>();

    services.AddSingleton<RedgifsAuthProvider>(sp =>
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
    services.AddSingleton<IAppSettings, CliAppSettings>();
    services.AddSingleton<IUserNotificationService, ConsoleUserNotificationService>();
    services.AddSingleton<ISecretProtector, PlainTextSecretProtector>();

    services.AddSingleton<IFileNameStrategy, FileNameService>();
    services.AddSingleton<IFikfapApiClient, FikfapApiClient>();
    services.AddSingleton(new FikfapSession { Token = Guid.NewGuid().ToString() });
}

static void PrintUsage()
{
    Console.WriteLine("Usage:");
    Console.WriteLine("  dotnet run --project src/RedgifsDownloader.Cli/RedgifsDownloader.Cli.csproj -- crawl redgifs <username>");
}
