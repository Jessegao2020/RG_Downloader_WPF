using RedgifsDownloader.ApplicationLayer.Interfaces;
using RedgifsDownloader.Domain.Entities;
using System.IO;

namespace RedgifsDownloader.Infrastructure
{
    public class FileNameService : IFileNameStrategy
    {
        public string GenerateFileName(Video video) => Path.GetFileName(video.Url.AbsolutePath);
    }
}
