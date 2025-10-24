namespace RedgifsDownloader.Model
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
