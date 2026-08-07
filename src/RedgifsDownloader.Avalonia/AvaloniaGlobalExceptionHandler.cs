using System.Text;
using Avalonia.Threading;

namespace RedgifsDownloader.Avalonia;

internal static class AvaloniaGlobalExceptionHandler
{
    private static readonly string LogDir = Path.Combine(AppContext.BaseDirectory, "logs");
    private static int _isLogging;
    private static bool _backgroundHandlersRegistered;
    private static bool _uiHandlerRegistered;

    public static void RegisterBackgroundHandlers()
    {
        if (_backgroundHandlersRegistered)
            return;

        _backgroundHandlersRegistered = true;
        TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;
        AppDomain.CurrentDomain.UnhandledException += OnDomainUnhandledException;
    }

    public static void RegisterUiHandler()
    {
        if (_uiHandlerRegistered)
            return;

        _uiHandlerRegistered = true;
        Dispatcher.UIThread.UnhandledException += OnDispatcherUnhandledException;
    }

    public static void HandleFatal(string type, Exception? exception)
    {
        LogException(type, exception);
        Environment.FailFast(type, exception);
    }

    private static void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        HandleFatal("DispatcherUnhandledException", e.Exception);
    }

    private static void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        HandleFatal("UnobservedTaskException", e.Exception);
    }

    private static void OnDomainUnhandledException(object? sender, UnhandledExceptionEventArgs e)
    {
        LogException("UnhandledException", e.ExceptionObject as Exception);
    }

    private static void LogException(string type, Exception? exception)
    {
        if (Interlocked.Exchange(ref _isLogging, 1) != 0)
            return;

        try
        {
            Directory.CreateDirectory(LogDir);
            var logFile = Path.Combine(LogDir, $"crash_{DateTime.Now:yyyyMMdd_HHmmss_fff}.txt");

            var builder = new StringBuilder();
            builder.AppendLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] {type}");
            builder.AppendLine($"OS: {Environment.OSVersion}");
            builder.AppendLine($"Framework: {Environment.Version}");
            builder.AppendLine($"Process: {Environment.ProcessPath ?? "(unknown)"}");
            builder.AppendLine();

            if (exception is null)
            {
                builder.AppendLine("Exception was null");
            }
            else
            {
                builder.AppendLine(FormatExceptionChain(exception));
            }

            builder.AppendLine(new string('-', 80));
            File.WriteAllText(logFile, builder.ToString(), Encoding.UTF8);
        }
        catch
        {
            // Never throw from the crash logger itself.
        }
        finally
        {
            Volatile.Write(ref _isLogging, 0);
        }
    }

    private static string FormatExceptionChain(Exception exception)
    {
        var builder = new StringBuilder();
        Exception? current = exception;
        var level = 0;

        while (current is not null)
        {
            builder.AppendLine($"--- Exception Level {level} ---");
            builder.AppendLine($"Type: {current.GetType().FullName}");
            builder.AppendLine($"Message: {current.Message}");
            builder.AppendLine("StackTrace:");
            builder.AppendLine(current.StackTrace ?? "(null)");
            builder.AppendLine();

            current = current.InnerException;
            level++;
        }

        return builder.ToString();
    }
}
