using System.IO;
using System.Net;
using System.Net.Http;

namespace RedgifsDownloader.Services.Reddit
{
    public class RedditDownloadCoordinator
    {
        private readonly RedditApiService _api;
        private readonly HttpClient _http;
        private readonly RedditImageDownloadService _downloader;
        private readonly CookieContainer _cookieContainer;

        public RedditDownloadCoordinator(RedditApiService api, RedditImageDownloadService downloader)
        {
            _api = api;
            _downloader = downloader;
        }

        public async Task<(int downloaded, int skipped)> DownloadAllImagesAsync(string username,
                                                                                string baseDir,
                                                                                int concurrency = 8,
                                                                                Action<string>? OnStatus = null,
                                                                                Action<int>? OnDownloadedCountChanged = null)
        {
            var posts = _api.StreamUserImagePostsAsync(username);

            int downloaded = 0, skipped = 0;
            using var semaphore = new SemaphoreSlim(concurrency);

            var tasks = new List<Task>();

            await foreach (var post in posts)
            {
                if (string.IsNullOrEmpty(post.Url)) continue;

                string fileName = RedditFileService.MakeSafeFileName(post);
                string path = Path.Combine(baseDir, fileName);
                if (File.Exists(path)) { skipped++; continue; }

                await semaphore.WaitAsync();
                tasks.Add(Task.Run(async () =>
                {
                    try
                    {
                        bool success = await _downloader.DownloadAsync(post.Url, path);
                        if (success)
                        {
                            int newCount = Interlocked.Increment(ref downloaded);
                            OnStatus?.Invoke($"[Downloaded]: {fileName}.");
                            OnDownloadedCountChanged?.Invoke(downloaded);
                        }
                    }
                    catch (HttpRequestException ex)
                    {
                        OnStatus?.Invoke($"[Http Error]: {fileName} => {ex.StatusCode}.");
                    }
                    catch (Exception ex)
                    {
                        OnStatus?.Invoke($"[Error]: {fileName} => {ex.Message}.");
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
