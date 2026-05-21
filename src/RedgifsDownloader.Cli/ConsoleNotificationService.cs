using RedgifsDownloader.ApplicationLayer.Interfaces;

namespace RedgifsDownloader.Cli;

public class ConsoleNotificationService : IUserNotificationService
{
    public void ShowMessage(string message)
    {
        Console.WriteLine(message);
    }
}
