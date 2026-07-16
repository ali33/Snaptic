using SkiaSharp;
using Snaptic.Core.Display;

namespace Snaptic.Core.Abstractions;

/// <summary>Chụp màn hình. Hiện thực bởi tầng platform.</summary>
public interface IScreenCapture
{
    /// <summary>
    /// Chụp TOÀN BỘ desktop ảo (mọi monitor) thành một ảnh duy nhất trong không gian
    /// physical pixel toàn cục. Gọi ngay khi bấm phím tắt để "đóng băng" màn hình.
    /// </summary>
    SKBitmap CaptureAllMonitors();

    IReadOnlyList<MonitorInfo> GetMonitors();
}
