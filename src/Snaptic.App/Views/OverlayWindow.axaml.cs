using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media.Imaging;
using SkiaSharp;
using Snaptic.Core.Display;

namespace Snaptic.App.Views;

/// <summary>
/// Lớp phủ khoanh vùng. MỘT cửa sổ trên MỖI monitor — vì mỗi màn một hệ số scaling
/// riêng nên không thể dùng một cửa sổ trải ngang mà quy đổi toạ độ đúng được.
///
/// Cửa sổ hiển thị ảnh ĐÃ ĐÓNG BĂNG chứ không phải màn hình thật: nhờ vậy menu đang
/// mở không biến mất, không nháy hình, và vùng chọn khớp đúng thứ người dùng nhìn thấy.
/// </summary>
public partial class OverlayWindow : Window
{
    private readonly MonitorInfo _monitor;
    private Point? _dragStart;

    /// <summary>Hai góc vùng chọn, đã ở toạ độ physical TOÀN CỤC.</summary>
    public event Action<PhysicalPoint, PhysicalPoint>? SelectionMade;

    public event Action? Cancelled;

    /// <summary>Ctor rỗng cho XAML designer — không dùng lúc chạy.</summary>
    public OverlayWindow()
        : this(null, new MonitorInfo("design", new PhysicalRect(0, 0, 800, 600), 1.0))
    {
    }

    public OverlayWindow(SKBitmap? frozenForThisMonitor, MonitorInfo monitor)
    {
        InitializeComponent();
        _monitor = monitor;

        // Position tính bằng PHYSICAL pixel; Width/Height tính bằng LOGICAL.
        // Trộn lẫn hai đơn vị này là cách nhanh nhất để overlay lệch khỏi màn hình.
        Position = new PixelPoint(monitor.PhysicalBounds.X, monitor.PhysicalBounds.Y);
        Width = monitor.PhysicalBounds.Width / monitor.ScaleFactor;
        Height = monitor.PhysicalBounds.Height / monitor.ScaleFactor;

        if (frozenForThisMonitor is not null)
        {
            FrozenImage.Source = ToAvaloniaBitmap(frozenForThisMonitor);
            FrozenImage.Width = Width;
            FrozenImage.Height = Height;
        }

        DimLayer.Width = Width;
        DimLayer.Height = Height;

        Canvas.SetLeft(HintBox, Width / 2 - 130);
        Canvas.SetTop(HintBox, Height - 90);

        PointerPressed += OnPointerPressed;
        PointerMoved += OnPointerMoved;
        PointerReleased += OnPointerReleased;
        KeyDown += OnKeyDown;
    }

    private static Bitmap ToAvaloniaBitmap(SKBitmap skBitmap)
    {
        using var image = SKImage.FromBitmap(skBitmap);
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        using var stream = new MemoryStream(data.ToArray());
        return new Bitmap(stream);
    }

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            e.Handled = true;
            Cancelled?.Invoke();
        }
    }

    private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        // BẮT GIỮ con trỏ vào cửa sổ này. Thiếu bước này, kéo chuột sang màn hình khác
        // sẽ khiến sự kiện nhảy sang overlay của màn đó — cửa sổ này không bao giờ nhận
        // PointerReleased và thao tác kéo treo vĩnh viễn.
        //
        // Bắt giữ cũng làm ca vắt ngang hai màn ĐÚNG: mọi toạ độ tiếp theo đều được báo
        // trong không gian logical của CHÍNH cửa sổ này, kể cả khi con trỏ đã nằm vật lý
        // trên màn khác. Avalonia quy đổi physical -> logical bằng scale của màn này, nên
        // DpiMapper nhân ngược lại bằng đúng scale đó sẽ ra lại physical toàn cục chính xác.
        e.Pointer.Capture(this);

        _dragStart = e.GetPosition(RootCanvas);
        HintBox.IsVisible = false;
        SelectionRect.IsVisible = true;
        UpdateSelection(_dragStart.Value, _dragStart.Value);
    }

    private void OnPointerMoved(object? sender, PointerEventArgs e)
    {
        if (_dragStart is { } start)
            UpdateSelection(start, e.GetPosition(RootCanvas));
    }

    private void UpdateSelection(Point a, Point b)
    {
        Canvas.SetLeft(SelectionRect, Math.Min(a.X, b.X));
        Canvas.SetTop(SelectionRect, Math.Min(a.Y, b.Y));
        SelectionRect.Width = Math.Abs(b.X - a.X);
        SelectionRect.Height = Math.Abs(b.Y - a.Y);
    }

    private void OnPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (_dragStart is not { } start)
            return;

        var end = e.GetPosition(RootCanvas);
        _dragStart = null;

        // Quy đổi TỪNG ĐIỂM sang physical toàn cục, dùng DPI của CHÍNH monitor này.
        // Đây là chỗ mà một hệ số chung cho cả rect sẽ sai khi kéo vắt ngang hai màn.
        var a = DpiMapper.ToGlobalPhysical(_monitor, start.X, start.Y);
        var b = DpiMapper.ToGlobalPhysical(_monitor, end.X, end.Y);

        SelectionMade?.Invoke(a, b);
    }
}
