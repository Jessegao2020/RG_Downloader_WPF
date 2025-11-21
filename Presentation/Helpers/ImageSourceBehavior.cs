using System.Collections.Concurrent;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace RedgifsDownloader.Presentation.Helpers
{
    public static class ImageSourceBehavior
    {
        // 缓存已加载的图片，避免重复加载
        private static readonly ConcurrentDictionary<string, BitmapImage> _imageCache = new();
        // 正在加载的URL集合，避免重复加载
        private static readonly ConcurrentDictionary<string, Task<BitmapImage?>> _loadingTasks = new();

        public static readonly DependencyProperty AsyncImageSourceProperty =
            DependencyProperty.RegisterAttached(
                "AsyncImageSource",
                typeof(string),
                typeof(ImageSourceBehavior),
                new PropertyMetadata(null, OnAsyncImageSourceChanged));

        public static string GetAsyncImageSource(DependencyObject obj)
        {
            return (string)obj.GetValue(AsyncImageSourceProperty);
        }

        public static void SetAsyncImageSource(DependencyObject obj, string value)
        {
            obj.SetValue(AsyncImageSourceProperty, value);
        }

        private static async void OnAsyncImageSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not Image image)
                return;

            var imageUrl = e.NewValue as string;

            if (string.IsNullOrWhiteSpace(imageUrl))
            {
                image.Source = null;
                return;
            }

            // 检查缓存
            if (_imageCache.TryGetValue(imageUrl, out var cachedImage))
            {
                image.Source = cachedImage;
                return;
            }

            // 检查是否正在加载
            if (_loadingTasks.TryGetValue(imageUrl, out var loadingTask))
            {
                // 等待正在进行的加载完成
                var bitmap = await loadingTask;
                if (bitmap != null && GetAsyncImageSource(image) == imageUrl)
                {
                    image.Source = bitmap;
                }
                return;
            }

            // 开始加载
            var loadTask = LoadImageInternalAsync(image, imageUrl);
            _loadingTasks.TryAdd(imageUrl, loadTask);

            try
            {
                var bitmap = await loadTask;
                if (bitmap != null)
                {
                    _imageCache.TryAdd(imageUrl, bitmap);
                    if (GetAsyncImageSource(image) == imageUrl) // 确保URL没有改变
                    {
                        image.Source = bitmap;
                    }
                }
            }
            catch
            {
                if (GetAsyncImageSource(image) == imageUrl)
                {
                    image.Source = null;
                }
            }
            finally
            {
                // 加载完成后从正在加载的集合中移除
                _loadingTasks.TryRemove(imageUrl, out _);
            }
        }

        private static async Task<BitmapImage?> LoadImageInternalAsync(Image image, string imageUrl)
        {
            try
            {
                return await ImageLoaderHelper.LoadImageAsync(imageUrl);
            }
            catch
            {
                return null;
            }
        }
    }
}

