using System.Windows;
using RedgifsDownloader.ApplicationLayer.Interfaces;

namespace RedgifsDownloader.Infrastructure
{
    public class WpfUiDispatcher : IUiDispatcher
    {
        public void Invoke(Action action)
        {
            Application.Current.Dispatcher.Invoke(action);
        }

        public Task InvokeAsync(Action action)
        {
            return Application.Current.Dispatcher.InvokeAsync(action).Task;
        }
    }
}
