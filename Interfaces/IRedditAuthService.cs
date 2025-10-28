namespace RedgifsDownloader.Interfaces
{
    public interface IRedditAuthService
    {
        Task LoginAsync();

        Task<string> GetAccessTokenAsync();

        bool IsLoggedIn { get; }
    }
}
