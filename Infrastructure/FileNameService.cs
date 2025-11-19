using System.IO;
using RedgifsDownloader.ApplicationLayer.Interfaces;
using RedgifsDownloader.Domain.Entities;

namespace RedgifsDownloader.Infrastructure
{
    public class FileNameService : IFileNameStrategy
    {
        public string GenerateFileName(Video video) => Path.GetFileName(video.Id);
    }
}
