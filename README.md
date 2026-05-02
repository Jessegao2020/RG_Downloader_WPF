# Media Downloader
A generic media downloader for various NSFW social platforms. By entering a `username` and a click of a downlaod 
button, you could download media files to your local storage with ease.

## Features
- List all available videos of a selected user
- Adjsutable simultaneous downloads
- Real-time download progress and status
- `Advanced View` with videos thumbnail previews
- Similar images filtering 
- Duplicate images batch deletion and renaming


## Dependencies
1. **.NET 8+ Runtime**

2. **ffmpeg.exe** should be in one of the following locations:
- Application root folder
- /bin subfolder
- System PATH enviroment variables

## Reddit 截止日期最小验证
1. 打开 `Reddit` 页面并完成登录。
2. 勾选 `启用截止日期`，选择某一天（本地日期）。
3. 使用同一个用户名分别测试 `Image` 和 `Video` 模式下载。
4. 观察日志是否出现 `启用截止日期(UTC)`，并确认下载结果不包含早于该 UTC 时间的帖子。

## Contribute
Github page: https://github.com/Jessegao2020/RG_Downloader_WPF
