using RedgifsDownloader.ApplicationLayer.Interfaces;

namespace RedgifsDownloader.Cli;

/// <summary>
/// Temporary non-secure implementation for CLI/Linux core testing only.
/// Do not use this as a secure secret storage mechanism.
/// </summary>
public class PlainTextSecretProtector : ISecretProtector
{
    public byte[] Protect(byte[] data) => data;

    public byte[] Unprotect(byte[] protectedData) => protectedData;
}
