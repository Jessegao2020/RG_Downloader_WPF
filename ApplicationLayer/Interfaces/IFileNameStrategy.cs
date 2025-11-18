using RedgifsDownloader.Domain.Entities;

namespace RedgifsDownloader.ApplicationLayer.Interfaces
{
    public interface IFileNameStrategy
    {
        string GenerateFileName(Video video);
    }
}
