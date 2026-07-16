using SkiaSharp;
using Snaptic.Core.Links;
using ZXing;
using ZXing.SkiaSharp;

namespace Snaptic.Core.Barcodes;

/// <summary>
/// Đọc MỘT mã từ ảnh. Cố ý không đọc nhiều mã: thao tác khoanh vùng của người dùng
/// đã tự chọn ra thứ họ cần rồi (spec §2).
/// </summary>
public sealed class BarcodeDecoder
{
    private readonly BarcodeReader _reader = new()
    {
        AutoRotate = true,
        Options = new ZXing.Common.DecodingOptions
        {
            TryHarder = true,
            TryInverted = true,
            // Phải khớp bảng mã của BarcodeGenerator. Mặc định ZXing là ISO-8859-1,
            // bảng đó không có chữ Việt có dấu.
            CharacterSet = BarcodeGenerator.CharacterSet
        }
    };

    public BarcodeResult? Decode(SKBitmap image)
    {
        var result = _reader.Decode(image);
        if (result is null)
            return null;

        return new BarcodeResult(
            Format: result.BarcodeFormat.ToString(),
            Text: result.Text,
            IsQr: result.BarcodeFormat == BarcodeFormat.QR_CODE,
            IsHttpUrl: LinkValidator.IsOpenableUrl(result.Text));
    }
}
