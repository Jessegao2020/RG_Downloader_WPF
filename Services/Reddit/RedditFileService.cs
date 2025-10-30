using RedgifsDownloader.Model.Reddit;

namespace RedgifsDownloader.Services.Reddit
{
    public class RedditFileService
    {
        public static string MakeSafeFileName(RedditPost post)
        {
            string ext = System.IO.Path.GetExtension(post.Url);

            // 去掉 Windows 不允许的字符
            string safeTitle = string.Concat(post.Title.Split(System.IO.Path.GetInvalidFileNameChars())).Trim();

            if (safeTitle.Length > 80)
                safeTitle = safeTitle[..80];

            return $"{safeTitle}-{post.Id}{ext}";
        }
    }
}
