using Avalonia;
using Microsoft.Extensions.DependencyInjection;

namespace RedgifsDownloader.Avalonia;

internal static class Program
{
    public static ServiceProvider Services { get; private set; } = null!;

    [STAThread]
    public static void Main(string[] args)
    {
        Services = ServiceConfiguration.BuildProvider();
        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
        Services.Dispose();
    }

    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .LogToTrace();
}
