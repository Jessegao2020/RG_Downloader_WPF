namespace RedgifsDownloader.Domain.Interfaces
{
    public interface IRedditAuthService
    {
        bool IsLoggedIn { get; }

        Task<string> GetAccessTokenAsync();
    }
}
