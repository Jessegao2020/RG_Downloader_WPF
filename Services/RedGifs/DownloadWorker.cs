using RedgifsDownloader.Domain.Enums;
using System.Buffers;
using System.Diagnostics;
using System.IO;
using System.Net.Http;

namespace RedgifsDownloader.Services.RedGifs
{
    public class DownloadWorker
    {
        private static readonly HttpClient _httpClient = new();

        public async Task<DownloadResult> DownloadAsync(string url, string filePath, string authToken, CancellationToken ct, Action<double>? onProgress = null)
        {
            if (ct.IsCancellationRequested || string.IsNullOrEmpty(url))
                return new DownloadResult(VideoStatus.Canceled, 0);

            string tempPath = filePath + ".tmp";

            try
            {
                using var response = await SendRequestAsync(url, authToken, ct);
                response.EnsureSuccessStatusCode();

                Debug.WriteLine($"[HTTP] GET {url}");
                Debug.WriteLine($"[HTTP] Authorization: Bearer {authToken.Substring(0, Math.Min(authToken.Length, 10))}...");


                long totalBytes = response.Content.Headers.ContentLength ?? -1L;
                using var stream = await response.Content.ReadAsStreamAsync(ct);

                // 写入临时文件
                var status = await SaveToFileAsync(stream, tempPath, totalBytes, onProgress, ct);

                if (status == VideoStatus.Completed)
                {
                    // 原子替换
                    if (File.Exists(filePath))
                        File.Delete(filePath);

                    File.Move(tempPath, filePath);
                }
                else
                {
                    // 下载中断或失败则删除临时文件
                    if (File.Exists(tempPath))
                        File.Delete(tempPath);
                }

                return new DownloadResult(status, totalBytes);
            }
            catch (HttpRequestException)
            {
                CleanupTemp(tempPath);
                return new DownloadResult(VideoStatus.NetworkError, 0);
            }
            catch (Exception ex)
            {
                CleanupTemp(tempPath);
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

        private void CleanupTemp(string tempPath)
        {
            try
            {
                if (File.Exists(tempPath))
                    File.Delete(tempPath);
            }
            catch { }
        }

        public record DownloadResult(VideoStatus Status, long TotalBytes);
    }
}
