using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Http;
using RedgifsDownloader.Interfaces;

namespace RedgifsDownloader.Services.Reddit
{
    public class RedditDownloadCoordinator
    {
        private readonly IRedditApiService _api;
        private readonly HttpClient _http;
        private readonly RedditImageDownloadService _downloader;
        private readonly CookieContainer _cookieContainer;

        public RedditDownloadCoordinator(IRedditApiService api, RedditImageDownloadService downloader)
        {
            _api = api;
            _downloader = downloader;
        }

        public async Task<(int downloaded, int skipped)> DownloadAllImagesAsync(string username, string baseDir, int concurrency = 8)
        {
            Directory.CreateDirectory(baseDir);

            var posts = _api.StreamUserImagePostsAsync(username);
            int downloaded = 0, skipped = 0;
            using var semaphore = new SemaphoreSlim(concurrency);

            var tasks = new List<Task>();

            await foreach (var post in posts)
            {
                if (string.IsNullOrEmpty(post.Url)) continue;

                string path = Path.Combine(baseDir, Path.GetFileName(post.Url));
                if (File.Exists(path)) { skipped++; continue; }

                await semaphore.WaitAsync();
                tasks.Add(Task.Run(async () =>
                {
                    try
                    {
                        bool success = await _downloader.DownloadAsync(post.Url, path);
                        if (success) Interlocked.Increment(ref downloaded);
                    }
                    finally
                    {
                        semaphore.Release();
                    }
                }));
            }

            await Task.WhenAll(tasks);
            return (downloaded, skipped);
        }
    }
}
