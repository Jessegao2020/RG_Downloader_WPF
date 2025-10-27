using RedgifsDownloader.Interfaces;

namespace RedgifsDownloader.Services
{
    public class PropertySettingService : ISettingsService
    {
        public int MaxDownloadCount
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
