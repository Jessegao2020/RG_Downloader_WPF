using System.IO;
using RedgifsDownloader.ApplicationLayer.Settings;

namespace RedgifsDownloader.Infrastructure.Settings
{
    internal class AppSettings : IAppSettings
    {
        private static readonly string DefaultDownloadPath = Path.Combine(AppContext.BaseDirectory, "Downloads");

        public int MaxConcurrentDownloads
        {
            get => Properties.Settings.Default.MaxDownloadCount;
            set => Properties.Settings.Default.MaxDownloadCount = value;
        }

        public string DownloadDirectory
        {
            get => Properties.Settings.Default.DownloadDirectory;
            set => Properties.Settings.Default.DownloadDirectory = value;
        }

        public void Save() => Properties.Settings.Default.Save();
    }
}
