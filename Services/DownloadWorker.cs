using RedgifsDownloader.Model;
using System.Buffers;
using System.Diagnostics;
using System.IO;
using System.Net.Http;

namespace RedgifsDownloader.Services
{
    public class DownloadWorker
    {
        private static readonly HttpClient _httpClient = new();

        public async Task<DownloadResult> DownloadAsync(string url, string filePath, string authToken, CancellationToken ct, Action<double>? onProgress = null)
        {
            if (ct.IsCancellationRequested || string.IsNullOrEmpty(url))
                return new DownloadResult(VideoStatus.Canceled, 0);

            try
            {
                using var response = await SendRequestAsync(url, authToken, ct);  // 发送Http请求
                response.EnsureSuccessStatusCode();     // 确保服务器返回码为200正常，否则抛异常

                long totalBytes = response.Content.Headers.ContentLength ?? -1L;    // 读取文件总大小，如无返回-1
                using var stream = await response.Content.ReadAsStreamAsync(ct);    // 接收文件流

                return new DownloadResult(await SaveToFileAsync(stream, filePath, totalBytes, onProgress, ct), totalBytes); 
            }
            catch (HttpRequestException) { return new DownloadResult(VideoStatus.NetworkError, 0); }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                return new DownloadResult(VideoStatus.UnknownError, 0);
            }
        }

        private static async Task<HttpResponseMessage> SendRequestAsync(string url, string token, CancellationToken ct)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Add("User-Agent", "Mozilla/5.0");
            request.Headers.Add("Referer", "https://www.redgifs.com/");
            request.Headers.Add("Authorization", "Bearer " + token);
            request.Headers.Add("Accept", "application/json, text/plain, */*");
            return await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);
        }

        private static async Task<VideoStatus> SaveToFileAsync(Stream stream, string filePath, long totalBytes, Action<double>? onProgress, CancellationToken ct)
        {
            byte[] buffer = ArrayPool<byte>.Shared.Rent(32768);

            try
            {
                using var fs = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None, 32768, useAsync: true);
                long totalRead = 0;
                double lastPercent = 0;

                int read;
                while ((read = await stream.ReadAsync(buffer.AsMemory(0, buffer.Length), ct)) > 0)
                {
                    await fs.WriteAsync(buffer.AsMemory(0, read), ct);
                    totalRead += read;

                    if (totalBytes > 0)
                    {
                        double currentPercent = Math.Round((double)totalRead / (double)totalBytes * 100, 1);
                        if (currentPercent - lastPercent >= 1.0 || currentPercent == 100.0)
                        {
                            lastPercent = currentPercent;
                            onProgress?.Invoke(currentPercent);
                        }
                    }
                }
                return VideoStatus.Completed;
            }
            catch (OperationCanceledException) { return VideoStatus.Canceled; }
            catch (IOException) { return VideoStatus.WriteError; }
            finally { ArrayPool<byte>.Shared.Return(buffer); }
        }

        public record DownloadResult(VideoStatus Status, long TotalBytes);
    }
}
