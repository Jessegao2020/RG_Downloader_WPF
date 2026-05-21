using RedgifsDownloader.ApplicationLayer.Interfaces;

namespace RedgifsDownloader.Avalonia.Services;

public sealed class AvaloniaNotificationService : IUserNotificationService
{
    public void ShowMessage(string message)
    {
        Console.WriteLine(message);
    }
}
