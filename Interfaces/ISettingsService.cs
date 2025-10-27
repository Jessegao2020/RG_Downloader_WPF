namespace RedgifsDownloader.Interfaces
{
    public interface ISettingsService
    {
        int MaxDownloadCount { get; set; }
        string DownloadDirectory { get; set; }

        void Save();
    }
}
