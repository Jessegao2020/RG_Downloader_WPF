using System;
using System.Collections.Generic;
using System.Diagnostics;
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
        public async Task RunDownloadsAsync(IEnumerable<VideoItem> videos, int concurrency, CancellationToken ct, Action? onStatusUpdate = null)
        {
            string baseDir = _fileService.EnsureDownloadBaseDirectory();
            using var semaphore = new SemaphoreSlim(concurrency);

            var tasks = videos.Select(async video =>
            {
                await semaphore.WaitAsync(ct); // 排队等候

                try
                {
                    if (_fileService.Exists(video, baseDir))
                    {
                        video.Status = VideoStatus.Exists;
                        video.Progress = 100;
                        return;
                    }

                    string filePath = _fileService.GetVideoPath(video, baseDir);
                    video.Status = VideoStatus.Downloading;

                    var status = await _worker.DownloadAsync(video.Url!, filePath, video.Token, ct, p => video.Progress = p);
                    video.Status = status;
                }
                catch (OperationCanceledException)
                {
                    video.Status = VideoStatus.Canceled;
                }
                catch(Exception ex) 
                {
                    Debug.WriteLine(ex);
                    video.Status = VideoStatus.UnknownError;
                }
                finally
                {
                    semaphore.Release();
                    onStatusUpdate?.Invoke();  // REVIEW： 此处未搞清楚
                }
            });

            await Task.WhenAll(tasks);
        }
        #endregion
    }
}
