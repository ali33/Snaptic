using SkiaSharp;

namespace Snaptic.Core.Images;

/// <summary>
/// Ảnh đã cắt theo vùng người dùng khoanh, kèm thời điểm chụp.
///
/// CỐ Ý là class chứ không phải record. Record sinh sẵn cú pháp `with`, mà
/// `result with { CapturedAt = ... }` sẽ tạo instance thứ hai dùng CHUNG một
/// SKBitmap — dispose cái này là rút ảnh khỏi chân cái kia. Dòng đó trông hoàn
/// toàn bình thường ở nơi gọi nên cái bẫy rất khó thấy. Value-equality và `with`
/// không chỗ nào cần, nên bỏ record là bỏ luôn cái bẫy.
/// </summary>
public sealed class CaptureResult : IDisposable
{
    public CaptureResult(SKBitmap image, DateTimeOffset capturedAt)
    {
        Image = image;
        CapturedAt = capturedAt;
    }

    public SKBitmap Image { get; }

    public DateTimeOffset CapturedAt { get; }

    public void Dispose() => Image.Dispose();
}
