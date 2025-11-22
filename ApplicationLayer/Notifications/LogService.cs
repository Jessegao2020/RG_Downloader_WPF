using System.Windows;
using RedgifsDownloader.ApplicationLayer.Interfaces;

namespace RedgifsDownloader.ApplicationLayer.Notifications
{
    public class LogService : IUserNotificationService
    {
        public void ShowMessage(string message)
        {
            MessageBox.Show(message);
        }
    }
}
