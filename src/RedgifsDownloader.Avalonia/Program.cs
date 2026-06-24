using Avalonia;
using Microsoft.Extensions.DependencyInjection;

namespace RedgifsDownloader.Avalonia;

internal static class Program
{
    public static ServiceProvider Services { get; private set; } = null!;

    public static MainWindowViewModel MainViewModel { get; private set; } = null!;

    [STAThread]
    public static void Main(string[] args)
    {
        Services = ServiceConfiguration.BuildProvider();

        // Create the production view models before Avalonia initializes any
        // design-time infrastructure. This prevents a published Native AOT app
        // from ever receiving the no-op commands intended only for the previewer.
        MainViewModel = new MainWindowViewModel();

        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
        Services.Dispose();
    }

    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .LogToTrace();
}
