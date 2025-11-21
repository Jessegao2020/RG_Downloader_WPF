using RedgifsDownloader.ApplicationLayer.DTOs;
using RedgifsDownloader.Domain.Enums;
using RedgifsDownloader.Domain.Interfaces;
using System.Buffers;
using System.Diagnostics;
using System.IO;
using System.Net.Http;

namespace RedgifsDownloader.Infrastructure
{
    public class HttpTransferDownloader : ITransferDownloader
    {
        private readonly HttpClient _http;
        public HttpTransferDownloader(HttpClient http)
        {
            _http = http;
        }

        public async Task<DownloadResult> DownloadAsync(
            Uri url,
            string outputPath,
            MediaDownloadContext context,
            CancellationToken ct = default,
            IProgress<double>? progress = null)
        {
            string tempPath = null;

            try
            {
                using var response = await SendRequestAsync(url, context, ct);
                response.EnsureSuccessStatusCode();

                // 临时命名逻辑，解决文件名没后缀的问题
                var contentType = response.Content.Headers.ContentType?.MediaType;
                string ext = contentType switch
                {
                    "video/mp4" => ".mp4",
                    "image/jpeg" => ".jpg",
                    "image/png" => ".png",
                    _ => ".bin"
                };
                long totalBytes = response.Content.Headers.ContentLength ?? -1;
                using var stream = await response.Content.ReadAsStreamAsync(ct);

                tempPath = outputPath + ext + ".tmp";
                var status = await SaveToFileAsync(stream, tempPath, totalBytes, progress, ct);

                if (status == VideoStatus.Completed)
                {
                    if (File.Exists(outputPath))
                        File.Delete(outputPath);

                    File.Move(tempPath, outputPath + ext); // 临时解决命后缀问题
                }
                else DeleteTempFile(tempPath);

                return new DownloadResult(status, totalBytes);
            }
            catch (HttpRequestException)
            {
                DeleteTempFile(tempPath);
                return new DownloadResult(VideoStatus.NetworkError, 0);
            }
            catch (OperationCanceledException)
            {
                DeleteTempFile(tempPath);
                return new DownloadResult(VideoStatus.Canceled, 0);
            }
            catch
            {
                DeleteTempFile(tempPath);
                return new DownloadResult(VideoStatus.UnknownError, 0);
            }
        }

        private async Task<HttpResponseMessage> SendRequestAsync(Uri url, MediaDownloadContext context, CancellationToken ct)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, url);

            if (context.Headers != null)
            {
                foreach (var (key, value) in context.Headers)
                    request.Headers.TryAddWithoutValidation(key, value);
            }

            return await _http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);
        }

        private async Task<VideoStatus> SaveToFileAsync(
            Stream stream,
            string path,
            long totalBytes,
            IProgress<double>? progress,
            CancellationToken ct)
        {
            byte[] buffer = ArrayPool<byte>.Shared.Rent(32768);

            try
            {
                using var file = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None, 32768, true);
                long readBytes = 0;
                double lastPercent = 0;

                int read;
                while ((read = await stream.ReadAsync(buffer.AsMemory(0, buffer.Length), ct)) > 0)
                {
                    await file.WriteAsync(buffer.AsMemory(0, read), ct);
                    readBytes += read;

                    if (totalBytes > 0)
                    {
                        double percent = (double)readBytes / totalBytes * 100;
                        if (percent - lastPercent >= 1.0 || percent >= 100.0)
                        {
                            lastPercent = percent;
                            progress?.Report(percent);
                        }
                    }
                }
                return VideoStatus.Completed;
            }
            catch (OperationCanceledException) { return VideoStatus.Canceled; }
            catch (IOException) { return VideoStatus.WriteError; }
            finally { ArrayPool<byte>.Shared.Return(buffer); }
        }

        private void DeleteTempFile(string path)
        {
            try { if (File.Exists(path)) File.Delete(path); } catch { }
        }
    }
}



