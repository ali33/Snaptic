namespace Snaptic.Core.Display;

/// <summary>
/// Một màn hình. <paramref name="PhysicalBounds"/> là vị trí và kích thước
/// tính bằng pixel vật lý trong không gian desktop ảo toàn cục.
/// <paramref name="ScaleFactor"/> là hệ số scaling của riêng màn này (1.5 = 150%).
/// </summary>
public sealed record MonitorInfo(string Id, PhysicalRect PhysicalBounds, double ScaleFactor);
