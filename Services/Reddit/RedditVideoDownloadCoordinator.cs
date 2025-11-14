using RedgifsDownloader.Domain.Enums;
using RedgifsDownloader.Model;
using RedgifsDownloader.Services.RedGifs;

namespace RedgifsDownloader.Services.Reddit
{
    public class RedditVideoDownloadCoordinator
    {
        private readonly DownloadWorker _worker;
        private readonly VideoFileService _fileService;

        public event Action<string>? OnStatus;
        public event Action<int>? OnProgress;

        public RedditVideoDownloadCoordinator(DownloadWorker worker, VideoFileService fileService)
        {
            _worker = worker;
            _fileService = fileService;
        }

        public async Task<(int downloaded, int failed)> DownloadStreamAsync(
            IAsyncEnumerable<VideoItem> videoStream,
            int concurrency,
            CancellationToken ct)
        {
            string baseDir = _fileService.EnsureDownloadBaseDirectory();
            var semaphore = new SemaphoreSlim(concurrency);

            int completed = 0, failed = 0;
            var allTasks = new List<Task>();

            await foreach (var video in videoStream.WithCancellation(ct))
            {
                if (string.IsNullOrEmpty(video.Url))
                    continue;

                await semaphore.WaitAsync(ct);

                allTasks.Add(Task.Run(async () =>
                {
                    try
                    {
                        string filePath = _fileService.GetVideoPath(video, baseDir);
                        if (_fileService.Exists(video, baseDir))
                        {
                            OnStatus?.Invoke($"[Skip] {System.IO.Path.GetFileName(filePath)} 已存在。");
                            Interlocked.Increment(ref completed);
                            return;
                        }

                        OnStatus?.Invoke($"[Start] {System.IO.Path.GetFileName(filePath)}");

                        var result = await _worker.DownloadAsync(video.Url!, filePath, video.Token, ct, null);
                        if (result.Status == VideoStatus.Completed)
                        {
                            int newCount = Interlocked.Increment(ref completed);
                            OnProgress?.Invoke(newCount);
                            OnStatus?.Invoke($"[OK] {System.IO.Path.GetFileName(filePath)}");
                        }
                        else
                        {
                            Interlocked.Increment(ref failed);
                            OnStatus?.Invoke($"[Fail] {System.IO.Path.GetFileName(filePath)} ({result.Status})");
                        }
                    }
                    catch (Exception ex)
                    {
                        Interlocked.Increment(ref failed);
                        OnStatus?.Invoke($"[Error] {video.Url}: {ex.Message}");
                    }
                    finally
                    {
                        semaphore.Release();
                    }
                }, ct));
            }

            await Task.WhenAll(allTasks);
            return (completed, failed);
        }
    }
}
