namespace RedgifsDownloader.Interfaces
{
    public interface IRedditApiService
    {
        Task<string> GetUserSubmittedAsync(string username);
    }
}
