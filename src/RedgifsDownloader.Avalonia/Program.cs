using Avalonia;
using Microsoft.Extensions.DependencyInjection;

namespace RedgifsDownloader.Avalonia;

internal static class Program
{
    public static ServiceProvider Services { get; private set; } = null!;

    [STAThread]
    public static void Main(string[] args)
    {
        AvaloniaGlobalExceptionHandler.RegisterBackgroundHandlers();

        try
        {
            Services = ServiceConfiguration.BuildProvider();
            BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
        }
        catch (Exception ex)
        {
            AvaloniaGlobalExceptionHandler.HandleFatal("Main", ex);
        }
        finally
        {
            Services?.Dispose();
        }
    }

    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .LogToTrace();
}
