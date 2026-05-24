using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;

namespace RedgifsDownloader.Avalonia.Views;

public partial class RedditView : UserControl
{
    public RedditView()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private void LogBox_OnTextChanged(object? sender, TextChangedEventArgs e)
    {
        if (sender is TextBox logBox)
        {
            logBox.CaretIndex = logBox.Text?.Length ?? 0;
        }
    }
}
