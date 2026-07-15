using SkiaSharp;

namespace Snaptic.Core.Abstractions;

/// <summary>Clipboard. Hiện thực bởi tầng platform.</summary>
public interface IClipboardService
{
    /// <summary>Ném <see cref="InvalidOperationException"/> nếu clipboard bị app khác giữ.</summary>
    void SetImage(SKBitmap image);

    void SetText(string text);
}
