namespace RedgifsDownloader.Domain.Enums
{
    public enum VideoStatus
    {
        Pending,
        Downloading,
        Completed,
        Exists,
        Canceled,
        NetworkError,
        WriteError,
        UnknownError,
        Failed
    }
}
