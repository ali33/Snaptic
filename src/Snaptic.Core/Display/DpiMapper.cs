namespace Snaptic.Core.Display;

/// <summary>
/// Quy đổi toạ độ giữa không gian logical (Avalonia dùng) và physical (ảnh chụp dùng).
///
/// QUAN TRỌNG: không gian logical BỊ CHIA KHÚC — mỗi monitor một hệ số scaling riêng,
/// nên KHÔNG tồn tại hệ số chung để quy đổi cả một vùng chọn. Phải quy đổi TỪNG ĐIỂM
/// theo DPI của monitor chứa chính điểm đó. Nếu không, vùng chọn vắt ngang hai màn
/// khác scaling sẽ sai — và bug đó chỉ lộ ra trên máy nhiều màn hình.
/// </summary>
public static class DpiMapper
{
    /// <summary>
    /// Đổi một điểm trong toạ độ logical CỤC BỘ của một monitor (gốc 0,0 là góc
    /// trên-trái của chính monitor đó) sang toạ độ physical TOÀN CỤC.
    /// </summary>
    public static PhysicalPoint ToGlobalPhysical(
        MonitorInfo monitor, double localLogicalX, double localLogicalY)
    {
        var x = monitor.PhysicalBounds.X + (int)Math.Round(localLogicalX * monitor.ScaleFactor);
        var y = monitor.PhysicalBounds.Y + (int)Math.Round(localLogicalY * monitor.ScaleFactor);
        return new PhysicalPoint(x, y);
    }

    /// <summary>
    /// Dựng hình chữ nhật từ hai điểm physical bất kỳ. Hai điểm có thể nằm trên hai
    /// monitor khác nhau — vì cả hai đã ở không gian physical toàn cục nên kết quả
    /// luôn xác định.
    /// </summary>
    public static PhysicalRect RectFrom(PhysicalPoint a, PhysicalPoint b)
        => new(
            Math.Min(a.X, b.X),
            Math.Min(a.Y, b.Y),
            Math.Abs(a.X - b.X),
            Math.Abs(a.Y - b.Y));
}
