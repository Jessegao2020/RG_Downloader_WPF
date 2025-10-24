﻿using RedgifsDownloader.Model;
using System.CodeDom.Compiler;
using System.Diagnostics;

namespace RedgifsDownloader.Services
{
    public record DownloadSummary(int Completed, int Failed);
    public class DownloadCoordinator
    {
        private readonly DownloadWorker _worker;
        private readonly VideoFileService _fileService;

        public DownloadCoordinator(DownloadWorker worker, VideoFileService fileService)
        {
            _worker = worker;
            _fileService = fileService;
        }
        
        public async Task<DownloadSummary> RunDownloadsAsync(IEnumerable<VideoItem> videos, int concurrency, CancellationToken ct, Action? onStatusUpdate = null)
        {
            string baseDir = _fileService.EnsureDownloadBaseDirectory();
            InitializeVideoStatuses(videos, baseDir);
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
                    onStatusUpdate?.Invoke();  
                }
            });

            await Task.WhenAll(tasks);
            int completed = videos.Count(v=>v.Status == VideoStatus.Completed);
            int failed = videos.Count(v => v.Status is VideoStatus.WriteError or VideoStatus.NetworkError or VideoStatus.UnknownError or VideoStatus.Canceled);
            return new DownloadSummary(completed, failed);
        }

        private void InitializeVideoStatuses(IEnumerable<VideoItem> videos, string baseDir)
        {
            foreach (var video in videos)
            {
                if (string.IsNullOrEmpty(video.Url)) continue;

                if (_fileService.Exists(video, baseDir))
                {
                    video.Status = VideoStatus.Exists;
                    video.Progress = 100;
                }
                else if (video.Status is VideoStatus.Completed or VideoStatus.Exists)
                {
                    video.Status = VideoStatus.Pending;
                }
            }
        }
    }
}
