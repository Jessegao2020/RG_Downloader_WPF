namespace RedgifsDownloader.ApplicationLayer.Interfaces
{
    public interface IUiDispatcher
    {
        void Invoke(Action action);
        Task InvokeAsync(Action action);
    }
}
