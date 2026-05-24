using Avalonia;
using Avalonia.Controls;

namespace RedgifsDownloader.Avalonia.Views;

public partial class ToastWindow : Window
{
    public ToastWindow()
    {
        InitializeComponent();
    }

    public static async void Show(Window? owner, string message)
    {
        var toast = new ToastWindow();
        toast.MessageText.Text = message;

        if (owner is not null)
        {
            try
            {
                var ownerPos = owner.Position;
                var ownerSize = owner.Bounds.Size;

                toast.Position = new PixelPoint(
                    ownerPos.X + (int)ownerSize.Width - 280,
                    ownerPos.Y + (int)ownerSize.Height - 120);
            }
            catch
            {
                // 定位失败也不要影响提示
            }

            toast.Show(owner);
        }
        else
        {
            toast.Show();
        }

        await Task.Delay(2000);

        if (toast.IsVisible)
        {
            toast.Close();
        }
    }
}
