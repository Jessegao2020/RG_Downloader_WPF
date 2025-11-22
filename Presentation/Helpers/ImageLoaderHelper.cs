using System.IO;
using System.Net.Http;
using System.Windows.Media.Imaging;

namespace RedgifsDownloader.Presentation.Helpers
{
    public static class ImageLoaderHelper
    {
        private static readonly HttpClient _httpClient = new HttpClient();

        public static async Task<BitmapImage?> LoadImageAsync(string? imageUrl)
        {
            if (string.IsNullOrWhiteSpace(imageUrl))
                return null;

            try
            {
                // 对于需要headers的URL（如Fikfap），使用HttpClient下载
                if (imageUrl.Contains("vz-5d293dac-178.b-cdn.net"))
                {
                    using var request = new HttpRequestMessage(HttpMethod.Get, imageUrl);
                    request.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/121.0.0.0 Safari/537.36");
                    request.Headers.Add("Referer", "https://fikfap.com/");
                    request.Headers.Add("Origin", "https://fikfap.com");
                    request.Headers.Add("Accept", "*/*");
                    request.Headers.Add("Accept-Language", "en-US,en;q=0.9");

                    using var response = await _httpClient.SendAsync(request);
                    if (!response.IsSuccessStatusCode)
                        return null;

                    // 读取到MemoryStream，避免stream被提前dispose
                    using var sourceStream = await response.Content.ReadAsStreamAsync();
                    var memoryStream = new MemoryStream();
                    await sourceStream.CopyToAsync(memoryStream);
                    memoryStream.Position = 0;

                    var bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.StreamSource = memoryStream;
                    bitmap.DecodePixelWidth = 300;
                    bitmap.EndInit();
                    bitmap.Freeze();

                    return bitmap;
                }
                else
                {
                    // 对于普通URL（如redgifs），也使用HttpClient下载，统一处理
                    using var response = await _httpClient.GetAsync(imageUrl);
                    if (!response.IsSuccessStatusCode)
                        return null;

                    // 读取到MemoryStream，避免stream被提前dispose
                    using var sourceStream = await response.Content.ReadAsStreamAsync();
                    var memoryStream = new MemoryStream();
                    await sourceStream.CopyToAsync(memoryStream);
                    memoryStream.Position = 0;

                    var bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.StreamSource = memoryStream;
                    bitmap.DecodePixelWidth = 300;
                    bitmap.EndInit();
                    bitmap.Freeze();

                    return bitmap;
                }
            }
            catch
            {
                return null;
            }
        }
    }
}

