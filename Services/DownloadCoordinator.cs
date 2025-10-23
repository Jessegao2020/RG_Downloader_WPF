using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RedgifsDownloader.Services
{
    public class DownloadCoordinator
    {
        private readonly DownloadWorker _worker;
        private readonly VideoFileService _fileService;

        public DownloadCoordinator(DownloadWorker worker, VideoFileService fileService)
        {
            _worker = worker;
            _fileService = fileService;
        }

        #region // 未审核区块
        public async Task RunDownloadsAsync(IEnumerable<VideoItem> videos, int concurrency, CancellationToken token, Action? onStatusUpdate = null)
        {
            string baseDir = _fileService.EnsureDownloadBaseDirectory();
            using var semaphore = new SemaphoreSlim(concurrency);

            var tasks = videos.Select(async video =>
            {
                await semaphore.WaitAsync(token);
                try
                {
                    if (_fileService.Exists(video, baseDir))
                    {
                        video.Status = VideoStatus.Exists;
                        video.Progress = 100;
                        return;
                    }

                    string path = _fileService.GetVideoPath(video, baseDir);
                    video.Status = VideoStatus.Downloading;

                    var result = await _worker.DownloadAsync(video.Url!, path, video.Token, token, p => video.Progress = p);
                    video.Status = result;
                }
                catch (OperationCanceledException)
                {
                    video.Status = VideoStatus.Canceled;
                }
                finally
                {
                    semaphore.Release();
                    onStatusUpdate?.Invoke();
                }
            });

            await Task.WhenAll(tasks);
        }
        #endregion
    }
}
