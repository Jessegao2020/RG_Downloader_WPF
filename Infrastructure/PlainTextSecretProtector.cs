using RedgifsDownloader.ApplicationLayer.Interfaces;

namespace RedgifsDownloader.Infrastructure
{
    /// <summary>
    /// 临时实现：仅用于 CLI/Linux 核心功能测试。
    /// 该实现不提供任何安全保护，不可用于生产环境机密存储。
    /// </summary>
    public class PlainTextSecretProtector : ISecretProtector
    {
        public byte[] Protect(byte[] data)
        {
            return data;
        }

        public byte[] Unprotect(byte[] protectedData)
        {
            return protectedData;
        }
    }
}
