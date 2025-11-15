using RedgifsDownloader.Domain.Enums;

namespace RedgifsDownloader.Domain.Interfaces
{
    public interface IPlatformAuthProvider
    {
        Task<PlatformAuthResult> GetTokenAsync(CancellationToken ct = default);
    }
}
