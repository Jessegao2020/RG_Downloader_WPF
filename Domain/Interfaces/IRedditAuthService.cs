namespace RedgifsDownloader.Domain.Interfaces
{
    public interface IRedditAuthService
    {
        bool IsLoggedIn { get; }

        Task LoginAsync();

        Task<string> GetAccessTokenAsync();
    }
}
