using System.IO;
using RedgifsDownloader.ApplicationLayer.Settings;
using RedgifsDownloader.Domain.Entities;
using RedgifsDownloader.Domain.Interfaces;

namespace RedgifsDownloader.ApplicationLayer.Downloads
{
    public class VideoPathStrategy : IVideoPathStrategy
    {
        private readonly IFileStorage _fileStorage;
        private readonly IAppSettings _settings;

        public VideoPathStrategy(IFileStorage fileStorage, IAppSettings settings)
        {
            _fileStorage = fileStorage;
            _settings = settings;
        }

        public string BuildDownloadPath(Video video)
        {
            string baseDir = _settings.DownloadDirectory;

            string username = string.IsNullOrWhiteSpace(video.Username) ? "Unknown" : video.Username;
                        
            string fileName = GenerateFileName(video);

            return _fileStorage.CombinePath(baseDir, username, fileName);
        }

        private string GenerateFileName(Video video)
        {
            return Path.GetFileName(video.Url.AbsolutePath);
        }
    }
}
