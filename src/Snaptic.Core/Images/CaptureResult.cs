using SkiaSharp;

namespace Snaptic.Core.Images;

/// <summary>Ảnh đã cắt theo vùng người dùng khoanh, kèm thời điểm chụp.</summary>
public sealed record CaptureResult(SKBitmap Image, DateTimeOffset CapturedAt) : IDisposable
{
    public void Dispose() => Image.Dispose();
}
