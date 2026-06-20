using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using Avalonia;
using Avalonia.Controls;

namespace RedgifsDownloader.Avalonia;

public partial class MainWindow : Window
{
    private static readonly string WindowStateFile =
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "RedgifsDownloader",
            "window-state.json");

    private WindowPlacement? _normalPlacement;

    public MainWindow()
    {
        InitializeComponent();
        DataContext = new MainWindowViewModel();

        Opened += (_, _) =>
        {
            RestoreWindowPlacement();

            CaptureNormalPlacement();

            PositionChanged += (_, _) => CaptureNormalPlacement();
            Resized += (_, _) => CaptureNormalPlacement();
        };

        Closing += (_, _) => SaveWindowPlacement();
    }

    private void CaptureNormalPlacement()
    {
        // 最大化或最小化时，Position/Width/Height 不是正常窗口的位置。
        if (WindowState != WindowState.Normal)
        {
            return;
        }

        _normalPlacement = new WindowPlacement
        {
            X = Position.X,
            Y = Position.Y,
            Width = Width,
            Height = Height,
            State = WindowState.Normal
        };
    }

    private void RestoreWindowPlacement()
    {
        try
        {
            if (!File.Exists(WindowStateFile))
            {
                return;
            }

            var json = File.ReadAllText(WindowStateFile);
            var placement = JsonSerializer.Deserialize<WindowPlacement>(json);

            if (placement is null)
            {
                return;
            }

            var savedPosition = new PixelPoint(placement.X, placement.Y);

            // 防止显示器被拔掉后，窗口恢复到屏幕之外。
            var targetScreen = Screens.All.FirstOrDefault(
                screen => screen.WorkingArea.Contains(savedPosition));

            if (targetScreen is null)
            {
                return;
            }

            Position = savedPosition;

            if (placement.Width >= MinWidth)
            {
                Width = placement.Width;
            }

            if (placement.Height >= MinHeight)
            {
                Height = placement.Height;
            }

            _normalPlacement = placement with
            {
                State = WindowState.Normal
            };

            WindowState = placement.State == WindowState.Minimized
                ? WindowState.Normal
                : placement.State;
        }
        catch
        {
            // 配置文件损坏时使用默认窗口位置，避免程序启动失败。
        }
    }

    private void SaveWindowPlacement()
    {
        try
        {
            if (WindowState == WindowState.Normal)
            {
                CaptureNormalPlacement();
            }

            var placement = _normalPlacement ?? new WindowPlacement
            {
                X = Position.X,
                Y = Position.Y,
                Width = Width,
                Height = Height
            };

            placement = placement with
            {
                // 不允许下次启动直接最小化。
                State = WindowState == WindowState.Minimized
                    ? WindowState.Normal
                    : WindowState
            };

            var directory = Path.GetDirectoryName(WindowStateFile);
            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var json = JsonSerializer.Serialize(
                placement,
                new JsonSerializerOptions
                {
                    WriteIndented = true
                });

            File.WriteAllText(WindowStateFile, json);
        }
        catch
        {
            // 保存失败不应该影响程序正常关闭。
        }
    }

    private sealed record WindowPlacement
    {
        public int X { get; init; }

        public int Y { get; init; }

        public double Width { get; init; } = 450;

        public double Height { get; init; } = 550;

        public WindowState State { get; init; } = WindowState.Normal;
    }
}