using RedgifsDownloader.Domain.Entities;

namespace RedgifsDownloader.ApplicationLayer.Notifications
{
    /// <summary>
    /// Application 层的视频变化通知服务
    /// 负责监控 Domain 实体变化并发出事件，避免 Domain 层耦合 UI
    /// </summary>
    public class VideoChangeNotifier
    {
        private readonly Dictionary<Video, int> _versionTracker = new();
        
        public event Action<Video>? VideoChanged;

        /// <summary>
        /// 订阅视频变化事件
        /// </summary>
        public void Subscribe(Action<Video> handler)
        {
            VideoChanged += handler;
        }

        /// <summary>
        /// 取消订阅视频变化事件
        /// </summary>
        public void Unsubscribe(Action<Video> handler)
        {
            VideoChanged -= handler;
        }

        /// <summary>
        /// 注册需要监控的视频
        /// </summary>
        public void RegisterVideo(Video video)
        {
            if (!_versionTracker.ContainsKey(video))
            {
                _versionTracker[video] = video.Version;
            }
        }

        /// <summary>
        /// 注销视频监控
        /// </summary>
        public void UnregisterVideo(Video video)
        {
            _versionTracker.Remove(video);
        }

        /// <summary>
        /// 检查视频变化并触发事件
        /// 此方法应在视频状态可能改变后调用（如下载进度更新）
        /// </summary>
        public void NotifyIfChanged(Video video)
        {
            if (_versionTracker.TryGetValue(video, out int lastVersion))
            {
                if (video.Version != lastVersion)
                {
                    _versionTracker[video] = video.Version;
                    VideoChanged?.Invoke(video);
                }
            }
        }

        /// <summary>
        /// 清空所有监控
        /// </summary>
        public void Clear()
        {
            _versionTracker.Clear();
        }
    }
}

