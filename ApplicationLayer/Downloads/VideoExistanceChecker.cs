using RedgifsDownloader.Domain.Interfaces;

namespace RedgifsDownloader.ApplicationLayer.Downloads
{
    public class VideoExistanceChecker
    {
        private readonly IFileStorage _storage;
        private readonly IVideoPathStrategy _pathStrategy;

        public VideoExistanceChecker(IFileStorage storage, IVideoPathStrategy pathStrategy)
        {
            _storage = storage;
            _pathStrategy = pathStrategy;
        }

        public bool Exists(DownloadContext context, bool strict = false)
        {
            var path = context.FilePath;

            if (!_storage.FileExists(path))
                return false;

            if (!strict)
                return true;

            if (!context.ExpectedSize.HasValue)
                return false;

            return _storage.GetFileSize(path) == context.ExpectedSize.Value; 
        }
    }
}
