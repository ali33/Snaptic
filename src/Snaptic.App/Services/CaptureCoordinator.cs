using SkiaSharp;
using Snaptic.App.Views;
using Snaptic.Core.Abstractions;
using Snaptic.Core.Display;
using Snaptic.Core.Images;

namespace Snaptic.App.Services;

/// <summary>
/// Điều phối luồng chụp: đóng băng màn hình → phủ overlay lên mọi monitor →
/// nhận vùng chọn → cắt.
/// </summary>
public sealed class CaptureCoordinator
{
    /// <summary>Nhỏ hơn ngưỡng này coi như lỡ tay click, không phải khoanh vùng.</summary>
    private const int MinimumSizePx = 8;

    private readonly IScreenCapture _capture;

    public CaptureCoordinator(IScreenCapture capture) => _capture = capture;

    /// <summary>Trả null khi người dùng huỷ (Esc) hoặc vùng chọn quá nhỏ.</summary>
    public async Task<CaptureResult?> CaptureRegionAsync()
    {
        // Đóng băng NGAY, trước khi bất cứ cửa sổ nào của ta hiện lên — nếu không
        // overlay sẽ tự lọt vào ảnh.
        using var frozen = _capture.CaptureAllMonitors();
        var monitors = _capture.GetMonitors();

        if (monitors.Count == 0)
            return null;

        // Gốc ảnh đóng băng trong toạ độ desktop ảo. CÓ THỂ ÂM khi màn phụ nằm bên
        // trái hoặc bên trên màn chính.
        var originX = monitors.Min(m => m.PhysicalBounds.X);
        var originY = monitors.Min(m => m.PhysicalBounds.Y);

        var tcs = new TaskCompletionSource<(PhysicalPoint A, PhysicalPoint B)?>();
        var overlays = new List<OverlayWindow>(monitors.Count);

        foreach (var monitor in monitors)
        {
            // OverlayWindow đổi sang Bitmap của Avalonia ngay trong ctor nên giải phóng
            // được luôn. Không dispose ở đây là rò vài MB MỖI LẦN chụp — app này nằm
            // tray cả ngày nên chỗ rò đó cộng dồn rất nhanh.
            using var slice = CropToRect(frozen, monitor.PhysicalBounds, originX, originY);
            var overlay = new OverlayWindow(slice, monitor);

            overlay.SelectionMade += (a, b) => tcs.TrySetResult((a, b));
            overlay.Cancelled += () => tcs.TrySetResult(null);

            overlays.Add(overlay);
        }

        try
        {
            foreach (var overlay in overlays)
                overlay.Show();

            overlays[0].Activate();

            var selection = await tcs.Task;

            if (selection is not { } sel)
                return null;

            var rect = DpiMapper.RectFrom(sel.A, sel.B);

            if (rect.Width < MinimumSizePx || rect.Height < MinimumSizePx)
                return null;

            return new CaptureResult(CropToRect(frozen, rect, originX, originY), DateTimeOffset.Now);
        }
        finally
        {
            // Đóng overlay TRƯỚC khi cửa sổ preview mở, và đóng kể cả khi có lỗi —
            // overlay Topmost mà kẹt lại thì khoá cứng màn hình người dùng.
            foreach (var overlay in overlays)
                overlay.Close();
        }
    }

    private static SKBitmap CropToRect(SKBitmap source, PhysicalRect rect, int originX, int originY)
    {
        // Đổi từ toạ độ desktop ảo sang toạ độ trong ảnh đóng băng.
        var x = rect.X - originX;
        var y = rect.Y - originY;

        // Kẹp vào biên ảnh — vùng chọn có thể tràn ra vùng không thuộc monitor nào
        // (desktop ảo là hình chữ nhật nhưng các màn có thể xếp so le).
        x = Math.Clamp(x, 0, Math.Max(0, source.Width - 1));
        y = Math.Clamp(y, 0, Math.Max(0, source.Height - 1));
        var w = Math.Clamp(rect.Width, 1, source.Width - x);
        var h = Math.Clamp(rect.Height, 1, source.Height - y);

        var result = new SKBitmap(w, h);
        source.ExtractSubset(result, new SKRectI(x, y, x + w, y + h));
        return result;
    }
}
