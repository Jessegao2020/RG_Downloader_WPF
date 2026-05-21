namespace RedgifsDownloader.ApplicationLayer.Interfaces
{
    public interface ISecretProtector
    {
        byte[] Protect(byte[] data);
        byte[] Unprotect(byte[] protectedData);
    }
}
