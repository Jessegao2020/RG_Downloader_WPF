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
        private readonly CookieContainer _cookieContainer;

        public RedditDownloadCoordinator(IRedditApiService api, RedditImageDownloadService downloader)
        {
            _api = api;

            // ✅ 统一 HttpClient：带 Cookie、User-Agent、长连接复用
            _cookieContainer = new CookieContainer();
            _cookieContainer.Add(new Uri("https://i.redd.it"), new Cookie("over18", "1"));

            var handler = new HttpClientHandler
            {
                UseCookies = true,
                CookieContainer = _cookieContainer
            };
            _http = new HttpClient(handler);
            _http.DefaultRequestHeaders.UserAgent.ParseAdd("RedgifsDownloader/1.0 (by u/test_user)");
        }

        public async Task<(int downloaded, int skipped)> DownloadAllImagesAsync(string username, string baseDir, int concurrency = 8)
        {
            int downloaded = 0;
            int skipped = 0;
            Directory.CreateDirectory(baseDir);

            using var semaphore = new SemaphoreSlim(concurrency);
            var tasks = new List<Task>();

            await foreach (var post in _api.StreamUserImagePostsAsync(username))
            {
                if (string.IsNullOrEmpty(post.Url))
                    continue;

                string fileName = Path.GetFileName(new Uri(post.Url).AbsolutePath);
                string filePath = Path.Combine(baseDir, fileName);

                // ✅ 已存在则跳过
                if (System.IO.File.Exists(filePath))
                {
                    Interlocked.Increment(ref skipped);
                    continue;
                }

                await semaphore.WaitAsync();

                var t = Task.Run(async () =>
                {
                    try
                    {
                        var bytes = await _http.GetByteArrayAsync(post.Url);
                        await System.IO.File.WriteAllBytesAsync(filePath, bytes);
                        Interlocked.Increment(ref downloaded);
                        Debug.WriteLine($"[Downloaded] {fileName}");
                    }
                    catch (HttpRequestException ex)
                    {
                        Debug.WriteLine($"[HTTP Error] {post.Url} => {ex.StatusCode}");
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"[Error] {post.Url} => {ex.Message}");
                    }
                    finally
                    {
                        semaphore.Release();
                    }
                });

                tasks.Add(t);
            }

            await Task.WhenAll(tasks);
            return (downloaded, skipped);
        }
    }
}
