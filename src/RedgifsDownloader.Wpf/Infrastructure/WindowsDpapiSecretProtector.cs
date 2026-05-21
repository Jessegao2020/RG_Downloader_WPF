using System.Security.Cryptography;
using RedgifsDownloader.ApplicationLayer.Interfaces;

namespace RedgifsDownloader.Infrastructure
{
    public class WindowsDpapiSecretProtector : ISecretProtector
    {
        public byte[] Protect(byte[] data)
        {
            return ProtectedData.Protect(data, null, DataProtectionScope.CurrentUser);
        }

        public byte[] Unprotect(byte[] protectedData)
        {
            return ProtectedData.Unprotect(protectedData, null, DataProtectionScope.CurrentUser);
        }
    }
}
