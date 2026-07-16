using Avalonia.Controls;
using Avalonia.Threading;

namespace Snaptic.App.Views;

/// <summary>
/// Thông báo ngắn tự tắt sau vài giây.
///
/// Spec §6 nói dùng "bong bóng tray", nhưng TrayIcon của Avalonia không phơi ra API
/// bong bóng, và gọi Shell_NotifyIcon với NIF_INFO thì cần sở hữu HWND của icon mà
/// Avalonia đang giữ. Toast tự vẽ chắc chắn chạy và không phụ thuộc nội bộ tray.
/// </summary>
public partial class ToastWindow : Window
{
    public ToastWindow() => InitializeComponent();

    private static void ShowFor(ToastWindow toast, TimeSpan duration)
        => DispatcherTimer.RunOnce(toast.Close, duration);

    public static void Show(Window owner, string message)
    {
        var toast = new ToastWindow();
        toast.MessageText.Text = message;
        toast.Show(owner);
        ShowFor(toast, TimeSpan.FromSeconds(3));
    }

    /// <summary>Toast không có cửa sổ cha — dùng lúc khởi động khi chưa có cửa sổ nào.</summary>
    public static void ShowStandalone(string message)
    {
        var toast = new ToastWindow
        {
            MessageText = { Text = message },
            WindowStartupLocation = WindowStartupLocation.CenterScreen
        };
        toast.Show();
        ShowFor(toast, TimeSpan.FromSeconds(6));
    }
}
