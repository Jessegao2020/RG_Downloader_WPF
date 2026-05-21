using Microsoft.Extensions.DependencyInjection;
using RedgifsDownloader.ApplicationLayer;
using RedgifsDownloader.ApplicationLayer.Downloads;
using RedgifsDownloader.ApplicationLayer.Fikfap;
using RedgifsDownloader.ApplicationLayer.Interfaces;
using RedgifsDownloader.ApplicationLayer.Notifications;
using RedgifsDownloader.ApplicationLayer.Settings;
using RedgifsDownloader.Cli;
using RedgifsDownloader.Domain.Entities;
using RedgifsDownloader.Domain.Enums;
using RedgifsDownloader.Domain.Interfaces;
using RedgifsDownloader.Infrastructure;
using RedgifsDownloader.Infrastructure.Fikfap;
using RedgifsDownloader.Infrastructure.Redgifs;
using RedgifsDownloader.Infrastructure.Settings;

if (args.Length >= 3 && string.Equals(args[0], "crawl", StringComparison.OrdinalIgnoreCase))
{
    if (TryParsePlatform(args[1], out var platform))
    {
        return await RunCrawlAsync(platform, args[2]);
    }
}

if (args.Length >= 3 && string.Equals(args[0], "download", StringComparison.OrdinalIgnoreCase))
{
    if (TryParsePlatform(args[1], out var platform))
    {
        return await RunDownloadAsync(platform, args);
    }
}

PrintUsage();
return 1;

static async Task<int> RunCrawlAsync(MediaPlatform platform, string username)
{
    using var provider = BuildProvider();
    var appService = provider.GetRequiredService<IDownloadAppService>();

    await foreach (var video in appService.CrawlAsync(platform, username))
    {
        Console.WriteLine($"Id={video.Id}");
        Console.WriteLine($"Url={video.Url}");
        Console.WriteLine($"CreateDateRaw={video.CreateDateRaw}");
        Console.WriteLine($"HasThumbnailUrl={!string.IsNullOrWhiteSpace(video.ThumbnailUrl)}");
        Console.WriteLine();
    }

    return 0;
}

static async Task<int> RunDownloadAsync(MediaPlatform platform, string[] args)
{
    var username = args[2];
    var output = Path.Combine(AppContext.BaseDirectory, "Downloads");
    var max = 3;

    for (var i = 3; i < args.Length; i++)
    {
        if (string.Equals(args[i], "--output", StringComparison.OrdinalIgnoreCase) && i + 1 < args.Length)
        {
            output = args[++i];
            continue;
        }

        if (string.Equals(args[i], "--max", StringComparison.OrdinalIgnoreCase) && i + 1 < args.Length && int.TryParse(args[++i], out var parsedMax) && parsedMax > 0)
        {
            max = parsedMax;
            continue;
        }

        PrintUsage();
        return 1;
    }

    if (platform == MediaPlatform.Fikfap && !TryFindFfmpegPath(out var ffmpegPath))
    {
        var ffmpegExecutable = OperatingSystem.IsWindows() ? "ffmpeg.exe" : "ffmpeg";
        Console.WriteLine($"Error: {ffmpegExecutable} was not found.");
        Console.WriteLine($"Please install FFmpeg and make sure '{ffmpegExecutable}' is available in PATH,");
        Console.WriteLine("or place it next to the CLI executable (or in a local 'bin' subdirectory).");
        return 3;
    }

    if (platform == MediaPlatform.Fikfap)
    {
        Console.WriteLine($"Using ffmpeg: {ffmpegPath}");
    }

    var resolvedOutput = Path.GetFullPath(output);
    Directory.CreateDirectory(resolvedOutput);

    using var provider = BuildProvider();
    var appService = provider.GetRequiredService<IDownloadAppService>();
    var appSettings = provider.GetRequiredService<IAppSettings>();
    var notifier = provider.GetRequiredService<VideoChangeNotifier>();

    appSettings.DownloadDirectory = resolvedOutput;

    var videos = new List<Video>();
    await foreach (var video in appService.CrawlAsync(platform, username))
    {
        videos.Add(video);
        if (videos.Count >= max)
        {
            break;
        }
    }

    if (videos.Count == 0)
    {
        Console.WriteLine("No videos found.");
        return 0;
    }

    notifier.Subscribe(video =>
    {
        Console.WriteLine($"[{video.Id}] Status={video.Status}, Progress={(video.Progress?.ToString("F1") ?? "-")}%");
    });

    foreach (var video in videos)
    {
        notifier.RegisterVideo(video);
    }

    Console.WriteLine($"Downloading {videos.Count} videos to: {resolvedOutput}");
    var summary = await appService.DownloadAsync(videos, 1);

    Console.WriteLine();
    Console.WriteLine("Final result:");
    foreach (var video in videos)
    {
        Console.WriteLine($"[{video.Id}] Status={video.Status}, Progress={(video.Progress?.ToString("F1") ?? "-")}%");
    }

    Console.WriteLine($"Summary: Completed={summary.Completed}, Failed={summary.Failed}");
    return summary.Failed > 0 ? 2 : 0;
}

static bool TryParsePlatform(string raw, out MediaPlatform platform)
{
    if (string.Equals(raw, "redgifs", StringComparison.OrdinalIgnoreCase))
    {
        platform = MediaPlatform.Redgifs;
        return true;
    }

    if (string.Equals(raw, "fikfap", StringComparison.OrdinalIgnoreCase))
    {
        platform = MediaPlatform.Fikfap;
        return true;
    }

    platform = default;
    return false;
}

static bool TryFindFfmpegPath(out string path)
{
    var executableName = OperatingSystem.IsWindows() ? "ffmpeg.exe" : "ffmpeg";

    var binPath = Path.Combine(AppContext.BaseDirectory, executableName);
    if (File.Exists(binPath))
    {
        path = binPath;
        return true;
    }

    var binSubPath = Path.Combine(AppContext.BaseDirectory, "bin", executableName);
    if (File.Exists(binSubPath))
    {
        path = binSubPath;
        return true;
    }

    var pathEnv = Environment.GetEnvironmentVariable("PATH");
    if (!string.IsNullOrWhiteSpace(pathEnv))
    {
        foreach (var entry in pathEnv.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries))
        {
            var fullPath = Path.Combine(entry, executableName);
            if (File.Exists(fullPath))
            {
                path = fullPath;
                return true;
            }
        }
    }

    path = string.Empty;
    return false;
}

static ServiceProvider BuildProvider()
{
    var services = new ServiceCollection();
    ConfigureServices(services);
    return services.BuildServiceProvider();
}

static void ConfigureServices(IServiceCollection services)
{
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
    services.AddSingleton<IAppSettings, CliAppSettings>();
    services.AddSingleton<IUserNotificationService, ConsoleNotificationService>();
    services.AddSingleton<ISecretProtector, PlainTextSecretProtector>();

    services.AddSingleton<IFileNameStrategy, FileNameService>();
    services.AddSingleton<IFikfapApiClient, FikfapApiClient>();
    services.AddSingleton(new FikfapSession { Token = Guid.NewGuid().ToString() });
}

static void PrintUsage()
{
    Console.WriteLine("Usage:");
    Console.WriteLine("  dotnet run --project src/RedgifsDownloader.Cli/RedgifsDownloader.Cli.csproj -- crawl redgifs <username>");
    Console.WriteLine("  dotnet run --project src/RedgifsDownloader.Cli/RedgifsDownloader.Cli.csproj -- crawl fikfap <username>");
    Console.WriteLine("  dotnet run --project src/RedgifsDownloader.Cli/RedgifsDownloader.Cli.csproj -- download redgifs <username> --output <folder> --max <count>");
    Console.WriteLine("  dotnet run --project src/RedgifsDownloader.Cli/RedgifsDownloader.Cli.csproj -- download fikfap <username> --output <folder> --max <count>");
}
