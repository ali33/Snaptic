using SkiaSharp;
using ZXing;
using ZXing.Common;
using ZXing.SkiaSharp;

namespace Snaptic.Core.Barcodes;

/// <summary>Tạo ảnh mã từ text. Cùng thư viện ZXing với <see cref="BarcodeDecoder"/>.</summary>
public static class BarcodeGenerator
{
    /// <summary>Bề rộng mong muốn cho mã 1D. Bề rộng thật được làm tròn về bội số nguyên của module.</summary>
    private const int TargetWidthPx = 400;

    private const int LinearHeightPx = 150;
    private const int QrSizePx = 400;

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
        var hints = new Dictionary<EncodeHintType, object>
        {
            [EncodeHintType.CHARACTER_SET] = CharacterSet
        };

        var (width, height) = MeasureCanvas(format, zxingFormat, text, hints);

        var writer = new BarcodeWriter
        {
            Format = zxingFormat,
            Options = new EncodingOptions
            {
                Width = width,
                Height = height,
                Margin = 2,
                PureBarcode = false
            }
        };

        foreach (var (key, value) in hints)
            writer.Options.Hints[key] = value;

        return writer.Write(text);
    }

    /// <summary>
    /// Kích thước canvas cần yêu cầu, khác nhau hẳn giữa QR và 1D.
    ///
    /// QR: KHÔNG đo gì cả. QRCodeWriter tự tính lại kích thước tự nhiên và bội số nguyên
    /// của chính nó, bỏ qua bề rộng ta yêu cầu — nên mọi phép tính module ở đây đều vô
    /// tác dụng. Đã kiểm chứng: QR đọc tốt ở bề rộng cố định bất kỳ, kể cả 400px không
    /// chia hết. Ép QR qua đường đo module chỉ tạo ra sai lệch mà không được gì.
    ///
    /// 1D: BẮT BUỘC đo. Barcode 1D mã hoá dữ liệu bằng đúng TỈ LỆ bề rộng vạch
    /// (1:2:3:4 module), và renderer 1D tin bề rộng ta đưa vô điều kiện. Bề rộng không
    /// chia hết cho số module thì mỗi biên bị làm tròn lệch, vạch 2-module có thể trông
    /// thành 3-module — mã nhìn vẫn như thật nhưng máy quét đọc sai hoặc không đọc được.
    /// Đã kiểm chứng với EAN-13 (113 module): 400px (3.54 px/module) KHÔNG đọc lại được;
    /// 339/452/565 (bội số đúng của 113) thì đọc tốt. Số module đổi theo nội dung với
    /// Code128/Code39 nên không ghim hằng số được — phải hỏi ZXing.
    ///
    /// Phép đo dùng ĐÚNG bộ hints như lúc render. Nếu không, hai lần encode có thể ra
    /// số module khác nhau và phép tính dựa trên số sai.
    /// </summary>
    private static (int Width, int Height) MeasureCanvas(
        SnapticFormat format, BarcodeFormat zxingFormat, string text,
        IDictionary<EncodeHintType, object> hints)
    {
        if (format == SnapticFormat.Qr)
            return (QrSizePx, QrSizePx);

        var moduleCount = new MultiFormatWriter().encode(text, zxingFormat, 0, 0, hints).Width;
        var scale = Math.Max(1, (int)Math.Round((double)TargetWidthPx / moduleCount));
        return (moduleCount * scale, LinearHeightPx);
    }

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
