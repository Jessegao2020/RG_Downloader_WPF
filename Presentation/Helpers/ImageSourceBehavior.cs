using System.Collections.Concurrent;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace RedgifsDownloader.Presentation.Helpers
{
    public static class ImageSourceBehavior
    {
        private static readonly Lazy<BitmapImage?> DefaultIcon = new(CreateDefaultIcon);
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
                SetFallback(image);
                return;
            }

            // 检查缓存
            if (_imageCache.TryGetValue(imageUrl, out var cachedImage))
            {
                // 缓存命中，立即设置（无闪烁）
                image.Source = cachedImage;
                return;
            }

            // 缓存未命中，清空旧图片，避免在容器回收时显示错误的图片
            SetFallback(image);

            // 检查是否正在加载
            if (_loadingTasks.TryGetValue(imageUrl, out var loadingTask))
            {
                // 等待正在进行的加载完成
                var bitmap = await loadingTask;
                if (GetAsyncImageSource(image) == imageUrl)
                {
                    if (bitmap != null)
                    {
                        image.Source = bitmap;
                    }
                    else
                    {
                        SetFallback(image);
                    }
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
                else if (GetAsyncImageSource(image) == imageUrl)
                {
                    SetFallback(image);
                }
            }
            catch
            {
                if (GetAsyncImageSource(image) == imageUrl)
                {
                    SetFallback(image);
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

        private static BitmapImage? CreateDefaultIcon()
        {
            try
            {
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = new Uri("pack://application:,,,/RedgifsDownloader.Wpf;component/Resources/icon.ico", UriKind.Absolute);
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.EndInit();
                bitmap.Freeze();
                return bitmap;
            }
            catch
            {
                return null;
            }
        }

        private static void SetFallback(Image image)
        {
            try
            {
                image.Source = DefaultIcon.Value;
            }
            catch
            {
                image.Source = null;
            }
        }
    }
}
