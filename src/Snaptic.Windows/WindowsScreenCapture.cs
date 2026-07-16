using System.Runtime.InteropServices;
using SkiaSharp;
using Snaptic.Core.Abstractions;
using Snaptic.Core.Display;
using Snaptic.Windows.Interop;

namespace Snaptic.Windows;

/// <summary>
/// Chụp toàn bộ desktop ảo bằng BitBlt.
/// Yêu cầu app khai báo PerMonitorV2 DPI awareness (xem app.manifest ở Snaptic.App),
/// nếu không GetSystemMetrics sẽ trả kích thước đã bị Windows ảo hoá và ảnh sẽ mờ/sai.
/// </summary>
public sealed class WindowsScreenCapture : IScreenCapture
{
    public SKBitmap CaptureAllMonitors()
    {
        var x = NativeMethods.GetSystemMetrics(NativeMethods.SM_XVIRTUALSCREEN);
        var y = NativeMethods.GetSystemMetrics(NativeMethods.SM_YVIRTUALSCREEN);
        var width = NativeMethods.GetSystemMetrics(NativeMethods.SM_CXVIRTUALSCREEN);
        var height = NativeMethods.GetSystemMetrics(NativeMethods.SM_CYVIRTUALSCREEN);

        if (width <= 0 || height <= 0)
            throw new InvalidOperationException(
                $"Kích thước desktop ảo không hợp lệ: {width}x{height}");

        var screenDc = NativeMethods.GetDC(IntPtr.Zero);
        if (screenDc == IntPtr.Zero)
            throw new InvalidOperationException("Không lấy được device context của màn hình");

        var memDc = NativeMethods.CreateCompatibleDC(screenDc);
        var hBitmap = NativeMethods.CreateCompatibleBitmap(screenDc, width, height);
        var oldObj = NativeMethods.SelectObject(memDc, hBitmap);

        try
        {
            var ok = NativeMethods.BitBlt(
                memDc, 0, 0, width, height,
                screenDc, x, y,
                NativeMethods.SRCCOPY | NativeMethods.CAPTUREBLT);

            if (!ok)
                throw new InvalidOperationException("BitBlt thất bại khi chụp màn hình");

            return ToSkBitmap(memDc, hBitmap, width, height);
        }
        finally
        {
            NativeMethods.SelectObject(memDc, oldObj);
            NativeMethods.DeleteObject(hBitmap);
            NativeMethods.DeleteDC(memDc);
            NativeMethods.ReleaseDC(IntPtr.Zero, screenDc);
        }
    }

    private static SKBitmap ToSkBitmap(IntPtr memDc, IntPtr hBitmap, int width, int height)
    {
        var info = new NativeMethods.BITMAPINFO
        {
            bmiHeader = new NativeMethods.BITMAPINFOHEADER
            {
                biSize = (uint)Marshal.SizeOf<NativeMethods.BITMAPINFOHEADER>(),
                biWidth = width,
                biHeight = -height,   // âm = top-down, khớp thứ tự hàng của Skia
                biPlanes = 1,
                biBitCount = 32,
                biCompression = 0     // BI_RGB
            }
        };

        var buffer = new byte[width * height * 4];
        var scanned = NativeMethods.GetDIBits(
            memDc, hBitmap, 0, (uint)height, buffer, ref info, NativeMethods.DIB_RGB_COLORS);

        if (scanned == 0)
            throw new InvalidOperationException("GetDIBits thất bại khi đọc pixel");

        // GDI trả BGRA; alpha từ BitBlt màn hình không đáng tin nên ép đục.
        for (var i = 3; i < buffer.Length; i += 4)
            buffer[i] = 255;

        var bitmap = new SKBitmap(
            new SKImageInfo(width, height, SKColorType.Bgra8888, SKAlphaType.Opaque));
        Marshal.Copy(buffer, 0, bitmap.GetPixels(), buffer.Length);
        return bitmap;
    }

    public IReadOnlyList<MonitorInfo> GetMonitors()
    {
        var monitors = new List<MonitorInfo>();

        NativeMethods.EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero,
            (IntPtr hMonitor, IntPtr _, ref NativeMethods.RECT _, IntPtr _) =>
            {
                var info = new NativeMethods.MONITORINFOEX
                {
                    cbSize = (uint)Marshal.SizeOf<NativeMethods.MONITORINFOEX>()
                };

                if (!NativeMethods.GetMonitorInfo(hMonitor, ref info))
                    return true; // bỏ qua monitor này, tiếp tục liệt kê

                var scale = 1.0;
                if (NativeMethods.GetDpiForMonitor(
                        hMonitor, NativeMethods.MONITOR_DPI_TYPE.MDT_EFFECTIVE_DPI,
                        out var dpiX, out _) == 0)
                {
                    scale = dpiX / 96.0;
                }

                var r = info.rcMonitor;
                monitors.Add(new MonitorInfo(
                    Id: info.szDevice,
                    PhysicalBounds: new PhysicalRect(r.Left, r.Top, r.Right - r.Left, r.Bottom - r.Top),
                    ScaleFactor: scale));

                return true;
            },
            IntPtr.Zero);

        return monitors;
    }
}
