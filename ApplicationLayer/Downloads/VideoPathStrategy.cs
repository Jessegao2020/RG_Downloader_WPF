using RedgifsDownloader.ApplicationLayer.Interfaces;
using RedgifsDownloader.ApplicationLayer.Settings;
using RedgifsDownloader.Domain.Entities;
using RedgifsDownloader.Domain.Interfaces;

namespace RedgifsDownloader.ApplicationLayer.Downloads
{
    public class VideoPathStrategy : IVideoPathStrategy
    {
        private readonly IFileStorage _fileStorage;
        private readonly IFileNameStrategy _fileNameStrategy;
        private readonly IAppSettings _settings;

        public VideoPathStrategy(IFileStorage fileStorage,  IAppSettings settings, IFileNameStrategy fileNameStrategy)
        {
            _fileStorage = fileStorage;
            _settings = settings;
            _fileNameStrategy = fileNameStrategy;
        }

        public string BuildDownloadPath(Video video)
        {
            string baseDir = _settings.DownloadDirectory;

            string username = string.IsNullOrWhiteSpace(video.Username) ? "Unknown" : video.Username;

            string fileName = _fileNameStrategy.GenerateFileName(video);

            return _fileStorage.CombinePath(baseDir, username, fileName);
        }
    }
}
