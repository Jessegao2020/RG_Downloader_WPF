using RedgifsDownloader.Model;
using System.Diagnostics;

namespace RedgifsDownloader.Services.RedGifs
{
    public record DownloadSummary(int Completed, int Failed);
    public class DownloadCoordinator
    {
        private readonly DownloadWorker _worker;
        private readonly VideoFileService _fileService;

        public event Action StatusUpdated;

        public DownloadCoordinator(DownloadWorker worker, VideoFileService fileService)
        {
            _worker = worker;
            _fileService = fileService;
        }

        public async Task<DownloadSummary> RunDownloadsAsync(IEnumerable<VideoItem> videos, int concurrency, bool strictCheck, CancellationToken ct)
        {
            string baseDir = _fileService.EnsureDownloadBaseDirectory();
            InitializeVideoStatuses(videos, baseDir, strictCheck);
            using var semaphore = new SemaphoreSlim(concurrency);

            var tasks = videos.Select(async video =>
            {
                await semaphore.WaitAsync(ct); // 排队等候

                try
                {
                    Debug.WriteLine($"[COORD] Start download: {video.Url}");
                    if (_fileService.Exists(video, baseDir, strictCheck))
                    {
                        video.Status = VideoStatus.Exists;
                        video.Progress = 100;
                        return;
                    }

                    string filePath = _fileService.GetVideoPath(video, baseDir);
                    video.Status = VideoStatus.Downloading;

                    var downloadResult = await _worker.DownloadAsync(video.Url!, filePath, video.Token, ct, p => video.Progress = p);
                    video.Status = downloadResult.Status;
                    if (downloadResult.TotalBytes > 0)
                        video.ExpectedSize = downloadResult.TotalBytes;
                }
                catch (OperationCanceledException)
                {
                    video.Status = VideoStatus.Canceled;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex);
                    video.Status = VideoStatus.UnknownError;
                }
                finally
                {
                    semaphore.Release();
                    StatusUpdated?.Invoke();
                    Debug.WriteLine($"[COORD] Finished: {video.Url} -> {video.Status}");

                }
            });

            await Task.WhenAll(tasks);

            int completed = videos.Count(video => video.Status == VideoStatus.Completed);
            int failed = videos.Count(video => video.Status is VideoStatus.WriteError or VideoStatus.NetworkError or VideoStatus.UnknownError or VideoStatus.Canceled);
            return new DownloadSummary(completed, failed);
        }

        private void InitializeVideoStatuses(IEnumerable<VideoItem> videos, string baseDir, bool strictCheck)
        {
            foreach (var video in videos)
            {
                if (string.IsNullOrEmpty(video.Url)) continue;

                if (_fileService.Exists(video, baseDir, strictCheck))
                {
                    video.Status = VideoStatus.Exists;
                    //video.Progress = 100;
                }
                else if (video.Status is VideoStatus.Completed or VideoStatus.Exists)
                {
                    video.Status = VideoStatus.Pending;
                    video.Progress = 0;
                }
            }
        } 
    }
}
