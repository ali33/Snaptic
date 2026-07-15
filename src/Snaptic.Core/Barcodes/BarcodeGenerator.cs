using SkiaSharp;
using ZXing;
using ZXing.Common;
using ZXing.SkiaSharp;

namespace Snaptic.Core.Barcodes;

/// <summary>Tạo ảnh mã từ text. Cùng thư viện ZXing với <see cref="BarcodeDecoder"/>.</summary>
public static class BarcodeGenerator
{
    /// <summary>Bề rộng mong muốn. Bề rộng thật sẽ được làm tròn về bội số nguyên của module.</summary>
    private const int TargetWidthPx = 400;

    private const int LinearHeightPx = 150;

    /// <summary>
    /// UTF-8 cho cả tạo lẫn đọc. Mặc định của ZXing là ISO-8859-1, bảng đó không có
    /// chữ Việt có dấu — "Tiếng Việt" sẽ ra "Ti?ng Vi?t".
    /// </summary>
    internal const string CharacterSet = "UTF-8";

    public static SKBitmap Generate(SnapticFormat format, string text)
    {
        var validation = BarcodeFormatSpec.Validate(format, text);
        if (!validation.IsValid)
            throw new ArgumentException(validation.Hint, nameof(text));

        var zxingFormat = ToZXing(format);

        // Bề rộng PHẢI là bội số nguyên của số module, nếu không mã sẽ không đọc lại được.
        // Xem ghi chú ở MeasureModuleCount.
        var moduleCount = MeasureModuleCount(zxingFormat, text);
        var scale = Math.Max(1, (int)Math.Round((double)TargetWidthPx / moduleCount));
        var width = moduleCount * scale;
        var height = format == SnapticFormat.Qr ? width : LinearHeightPx;

        var writer = new BarcodeWriter
        {
            Format = zxingFormat,
            Options = new EncodingOptions
            {
                Width = width,
                Height = height,
                Margin = 2,
                PureBarcode = false,
                Hints = { [EncodeHintType.CHARACTER_SET] = CharacterSet }
            }
        };

        return writer.Write(text);
    }

    /// <summary>
    /// Hỏi ZXing xem mã này cần bao nhiêu module, bằng cách encode ở kích thước tự nhiên
    /// (0,0 = một pixel mỗi module).
    ///
    /// VÌ SAO CẦN: barcode 1D mã hoá dữ liệu bằng đúng TỈ LỆ bề rộng vạch (1:2:3:4 module).
    /// Áp một bề rộng cố định không chia hết cho số module thì mỗi biên bị làm tròn lệch,
    /// và một vạch 2-module có thể trông thành 3-module — mã sinh ra nhìn vẫn như mã thật
    /// nhưng máy quét đọc ra sai hoặc không đọc được.
    ///
    /// Đo được bằng thực nghiệm với EAN-13 (113 module): bề rộng 400px (3.54 px/module)
    /// KHÔNG đọc lại được; 339/452/565 (bội số đúng của 113) thì đọc tốt. Số module thay
    /// đổi theo nội dung với Code128/Code39 nên không thể ghim một hằng số — phải hỏi.
    /// </summary>
    private static int MeasureModuleCount(BarcodeFormat format, string text)
        => RawWriterFor(format).encode(text, format, 0, 0).Width;

    private static Writer RawWriterFor(BarcodeFormat format) => format switch
    {
        BarcodeFormat.QR_CODE => new ZXing.QrCode.QRCodeWriter(),
        BarcodeFormat.CODE_128 => new ZXing.OneD.Code128Writer(),
        BarcodeFormat.EAN_13 => new ZXing.OneD.EAN13Writer(),
        BarcodeFormat.UPC_A => new ZXing.OneD.UPCAWriter(),
        BarcodeFormat.CODE_39 => new ZXing.OneD.Code39Writer(),
        _ => throw new ArgumentOutOfRangeException(nameof(format), format, "Định dạng không hỗ trợ")
    };

    private static BarcodeFormat ToZXing(SnapticFormat format) => format switch
    {
        SnapticFormat.Qr => BarcodeFormat.QR_CODE,
        SnapticFormat.Code128 => BarcodeFormat.CODE_128,
        SnapticFormat.Ean13 => BarcodeFormat.EAN_13,
        SnapticFormat.UpcA => BarcodeFormat.UPC_A,
        SnapticFormat.Code39 => BarcodeFormat.CODE_39,
        _ => throw new ArgumentOutOfRangeException(nameof(format), format, "Định dạng không hỗ trợ")
    };
}
