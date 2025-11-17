using System.IO;

namespace RedgifsDownloader.ApplicationLayer.Utils
{
    public static class FIleNameSanitizer
    {
        public static string MakeSafe(string raw)
        {
            if (string.IsNullOrEmpty(raw))
                return "untitled";

            var invalid = Path.GetInvalidFileNameChars();
            var cleaned = string.Concat(raw.Where(c => !invalid.Contains(c)));

            if (cleaned.Length == 0)
                cleaned = "untitled";

            if (cleaned.Length > 80)
                cleaned = cleaned[..80];

            return cleaned;
        }

        public static string MakeSafeFileName(string title, string id, string url)
        {
            string ext = Path.GetExtension(url) ?? ".jpg";
            string safeTitle = MakeSafe(title);

            return $"{safeTitle}-{id}{ext}";
        }
    }
}
