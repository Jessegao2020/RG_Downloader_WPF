using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Threading;

namespace RedgifsDownloader.Infrastructure
{
    public static class GlobalExceptionHandler
    {
        private static readonly string LogDir = Path.Combine(AppContext.BaseDirectory, "logs");
        private static bool _isLogging;

        public static void Register()
        {
            Application.Current.DispatcherUnhandledException += OnDispatcherUnhandledException;
            TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;
            AppDomain.CurrentDomain.UnhandledException += OnDomainUnhandledException;
        }

        private static void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            LogException("DispatcherUnhandledException", e.Exception);
            Environment.FailFast("UI thread crashed", e.Exception);
        }

        private static void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
        {
            LogException("UnobservedTaskException", e.Exception);
            Environment.FailFast("Task thread crashed", e.Exception);
        }

        private static void OnDomainUnhandledException(object? sender, UnhandledExceptionEventArgs e)
        {
            LogException("UnhandledException", e.ExceptionObject as Exception);
            Environment.FailFast("Domain unhandled exception", e.ExceptionObject as Exception);
        }

        private static void LogException(string type, Exception? ex)
        {
            if (_isLogging) return;
            _isLogging = true;

            try
            {
                Directory.CreateDirectory(LogDir);
                string logFile = Path.Combine(LogDir, $"crash_{DateTime.Now:yyyyMMdd_HHmmss}.txt");
                var sb = new StringBuilder();
                sb.AppendLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {type}");
                if (ex != null)
                {
                    sb.AppendLine($"Type: {ex.GetType().FullName}");
                    sb.AppendLine($"Message: {ex.Message}");
                    sb.AppendLine($"StackTrace:\n{ex.StackTrace}");
                }
                else sb.AppendLine("Exception was null");

                sb.AppendLine(new string('-', 80));
                File.WriteAllText(logFile, sb.ToString(), Encoding.UTF8);
            }
            catch { }
            finally
            {
                _isLogging = false;
            }
        }
    }
}
