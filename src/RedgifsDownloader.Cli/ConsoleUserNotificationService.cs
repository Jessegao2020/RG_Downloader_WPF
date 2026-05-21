using RedgifsDownloader.ApplicationLayer.Interfaces;

namespace RedgifsDownloader.Cli;

public class ConsoleUserNotificationService : IUserNotificationService
{
    public void ShowMessage(string message)
    {
        Console.WriteLine(message);
    }
}
