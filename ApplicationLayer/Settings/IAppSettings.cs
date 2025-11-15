namespace RedgifsDownloader.ApplicationLayer.Settings
{
    public interface IAppSettings
    {
        int MaxConcurrentDownloads { get; set; }
        string DownloadDirectory {  get; set; }
        void Save();
    }
}
