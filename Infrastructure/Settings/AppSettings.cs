using RedgifsDownloader.ApplicationLayer.Settings;

namespace RedgifsDownloader.Infrastructure.Settings
{
    internal class AppSettings : IAppSettings
    {
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
