# 通用 M3U8 下载器

## 架构说明

本模块提供了一个通用的 M3U8 下载器，支持音视频分离流的下载和 FFmpeg 合并。通过实现 `IStreamSelector` 接口，可以轻松适配不同网站的 M3U8 流选择策略。

## 核心组件

### 1. `IStreamSelector` 接口
定义流选择策略的接口：
```csharp
public interface IStreamSelector
{
    // 选择最佳视频流
    Uri? SelectBestVideoStream(string masterM3u8, Uri baseUrl);
    
    // 提取音频流
    Uri? ExtractAudioStream(string masterM3u8, Uri baseUrl);
}
```

### 2. `GenericM3u8Downloader` 通用下载器
实现了完整的 M3U8 下载逻辑：
- 下载主 M3U8 playlist
- 使用 `IStreamSelector` 选择视频和音频流
- 下载所有分段（支持 `.ts`, `.m4s`, `.mp4`, `.m4a`）
- 使用 FFmpeg 合并音视频（`-c copy`，不重新编码）
- 流式写入文件（低内存占用）
- 支持进度报告

### 3. `AvcPreferredStreamSelector` AVC 优先选择器
已实现的流选择器示例：
- 优先选择 AVC1 (H.264) 编码
- 排除 VP9 和 HEVC
- 按分辨率降序排序
- 支持分离音频流

## 如何为其他网站创建下载器

### 方式 1：使用现有的 `AvcPreferredStreamSelector`

如果网站的 M3U8 格式与 Fikfap 类似（标准 HLS 格式），直接使用：

```csharp
public class OtherSiteM3u8Downloader : ITransferDownloader
{
    private readonly GenericM3u8Downloader _downloader;

    public OtherSiteM3u8Downloader(HttpClient httpClient)
    {
        // 使用 AVC 优先的流选择策略
        var streamSelector = new AvcPreferredStreamSelector();
        _downloader = new GenericM3u8Downloader(httpClient, streamSelector);
    }

    public async Task<DownloadResult> DownloadAsync(
        Uri url, 
        string outputPath, 
        MediaDownloadContext context, 
        CancellationToken ct = default, 
        IProgress<double>? progress = null)
    {
        return await _downloader.DownloadAsync(url, outputPath, context, ct, progress);
    }
}
```

### 方式 2：实现自定义流选择器

如果网站有特殊的流选择需求，创建自定义选择器：

```csharp
public class HighestBitrateStreamSelector : IStreamSelector
{
    private static readonly Regex VariantRegex = new(@"#EXT-X-STREAM-INF:([^\n]+)\n([^\n]+)", RegexOptions.Compiled);
    private static readonly Regex AudioRegex = new(@"#EXT-X-MEDIA:.*?TYPE=AUDIO.*?URI=""([^""]+)""", RegexOptions.Compiled);

    public Uri? SelectBestVideoStream(string masterM3u8, Uri baseUrl)
    {
        var variants = new List<(int bandwidth, string url)>();
        var matches = VariantRegex.Matches(masterM3u8);

        foreach (Match match in matches)
        {
            var streamInfo = match.Groups[1].Value;
            var variantUrl = match.Groups[2].Value.Trim();

            // 解析带宽
            var bandwidthMatch = Regex.Match(streamInfo, @"BANDWIDTH=(\d+)");
            if (bandwidthMatch.Success && int.TryParse(bandwidthMatch.Groups[1].Value, out int bandwidth))
            {
                var fullUrl = new Uri(baseUrl, variantUrl).ToString();
                variants.Add((bandwidth, fullUrl));
            }
        }

        // 选择带宽最高的
        var best = variants.OrderByDescending(v => v.bandwidth).FirstOrDefault();
        return best.url != null ? new Uri(best.url) : null;
    }

    public Uri? ExtractAudioStream(string masterM3u8, Uri baseUrl)
    {
        var match = AudioRegex.Match(masterM3u8);
        if (match.Success)
        {
            var audioPath = match.Groups[1].Value;
            return new Uri(baseUrl, audioPath);
        }
        return null;
    }
}
```

### 方式 3：无音频流的简单场景

如果视频没有分离的音频流：

```csharp
public class SimpleStreamSelector : IStreamSelector
{
    public Uri? SelectBestVideoStream(string masterM3u8, Uri baseUrl)
    {
        // ... 选择视频流逻辑 ...
    }

    public Uri? ExtractAudioStream(string masterM3u8, Uri baseUrl)
    {
        // 返回 null 表示没有音频流
        return null;
    }
}
```

## 在 DI 中注册

在 `App.xaml.cs` 中注册新的下载器：

```csharp
// 为新网站创建专用 HttpClient 和下载器
services.AddSingleton<OtherSiteM3u8Downloader>(sp =>
{
    var httpClient = new HttpClient();
    return new OtherSiteM3u8Downloader(httpClient);
});
```

## 支持的 M3U8 格式

- **HLS (HTTP Live Streaming)** 标准格式
- **初始化分段**：`#EXT-X-MAP:URI="init.m4s"`
- **媒体分段**：`.ts`, `.m4s`, `.mp4`, `.m4a`
- **多变体流**：`#EXT-X-STREAM-INF`
- **音频流**：`#EXT-X-MEDIA:TYPE=AUDIO`

## 性能特点

- ✅ 低内存占用（流式写入，不缓存整个文件）
- ✅ 高并发支持（15+ 并发下载）
- ✅ 快速合并（FFmpeg `-c copy`，100ms 内完成）
- ✅ 进度报告（0-100% 连续进度）

## 示例：当前实现

### Fikfap 下载器
```csharp
public class FikfapM3u8Downloader : ITransferDownloader
{
    private readonly GenericM3u8Downloader _downloader;

    public FikfapM3u8Downloader(HttpClient httpClient)
    {
        var streamSelector = new AvcPreferredStreamSelector();
        _downloader = new GenericM3u8Downloader(httpClient, streamSelector);
    }

    public async Task<DownloadResult> DownloadAsync(...)
    {
        return await _downloader.DownloadAsync(...);
    }
}
```

只需要 **3 行核心代码**，就能复用完整的 M3U8 下载功能！

## 注意事项

1. **FFmpeg 依赖**：确保 `ffmpeg.exe` 在以下位置之一：
   - 程序 bin 目录
   - 程序 bin/bin 子目录
   - 系统 PATH 环境变量

2. **压缩处理**：通用下载器会自动设置 `Accept-Encoding: identity` 以避免 M3U8 内容被压缩

3. **错误处理**：自动处理网络错误、取消操作等异常情况

4. **临时文件**：自动清理下载过程中的临时文件


