using System.Windows;
using RedgifsDownloader.Interfaces;

namespace RedgifsDownloader.Services
{
    public class LogService : ILogService
    {
        public void ShowMessage(string message)
        {
            MessageBox.Show(message);
        }
    }
}
