using RedgifsDownloader.Domain.Interfaces;
using System.Windows;

namespace RedgifsDownloader.Infrastructure
{
    public class LogService : ILogService
    {
        public void ShowMessage(string message)
        {
            MessageBox.Show(message);
        }
    }
}
