namespace RedgifsDownloader.Domain.Interfaces
{
    public interface IDupeCleanerService
    {
        Task<(int kept, int deleted, List<string> logs)> CleanAsync(string folder);
    }
}
