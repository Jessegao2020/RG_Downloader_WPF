using System.Windows;
using System.Windows.Threading;

namespace RedgifsDownloader.View
{
    public partial class ToastWindow : Window
    {
        private static ToastWindow _currentToast = null;

        public ToastWindow(string message)
        {
            InitializeComponent();
            TextBlockMessage.Text = message;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            PositionNearMouse();

            var timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1.5) };
            timer.Tick += (s, _) => { timer.Stop(); Close(); };
            timer.Start();
        }

        public static void Show(string message)
        {
            // 如果旧的还活着，把它关掉
            _currentToast?.Close();

            // 创建新的
            var newToast = new ToastWindow(message);
            _currentToast = newToast;

            // 监听关闭事件，当它自然消失（时间到了）时，把引用清空
            newToast.Closed += (s, e) =>
            {
                if (_currentToast == newToast)
                    _currentToast = null;
            };

            newToast.Show();
        }

        private void PositionNearMouse()
        {
            // ================= 配置区域 =================
            // 正数向右/下，负数向左/上
            double margin = 15; // 基础间距，比如 15 像素

            // 1. 想要【右下角】(默认风格):
            // double offsetX = margin;
            // offsetY = margin;

            // 2. 想要【右上角】:
            double offsetX = -margin;
            double offsetY = -ActualHeight - margin;

            // 3. 想要【鼠标正下方】(水平居中):
            // double offsetX = -(this.ActualWidth / 2);
            // double offsetY = margin + 10; // +10 为了避开鼠标本身的高度

            // ===========================================

            // --- 以下是通用逻辑 (保留了 DPI 修正) ---
            var mousePos = System.Windows.Forms.Cursor.Position;

            // 获取 DPI 缩放
            var source = PresentationSource.FromVisual(this);
            double dpiX = source?.CompositionTarget?.TransformToDevice.M11 ?? 1.0;
            double dpiY = source?.CompositionTarget?.TransformToDevice.M22 ?? 1.0;

            // 鼠标物理坐标 -> WPF 逻辑坐标
            double wpfMouseX = mousePos.X / dpiX;
            double wpfMouseY = mousePos.Y / dpiY;

            // 计算目标位置
            double newLeft = wpfMouseX + offsetX;
            double newTop = wpfMouseY + offsetY;

            // --- 智能防越界 (让窗口永远保持在屏幕内) ---
            var screen = System.Windows.Forms.Screen.FromPoint(mousePos);
            double screenRight = (screen.WorkingArea.X + screen.WorkingArea.Width) / dpiX;
            double screenBottom = (screen.WorkingArea.Y + screen.WorkingArea.Height) / dpiY;
            double screenLeft = screen.WorkingArea.X / dpiX;
            double screenTop = screen.WorkingArea.Y / dpiY;

            // 右边界检查：如果超出右边，就改到鼠标左边显示
            if (newLeft + this.ActualWidth > screenRight)
                newLeft = wpfMouseX - this.ActualWidth - margin;

            // 下边界检查：如果超出底边，就改到鼠标上边显示
            if (newTop + this.ActualHeight > screenBottom)
                newTop = wpfMouseY - this.ActualHeight - margin;

            // 左边界检查 (防止因为偏移变为负数)
            if (newLeft < screenLeft) newLeft = screenLeft + margin;

            // 上边界检查
            if (newTop < screenTop) newTop = screenTop + margin;

            this.Left = newLeft;
            this.Top = newTop;
        }
    }
}
