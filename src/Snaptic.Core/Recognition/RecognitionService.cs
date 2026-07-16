using SkiaSharp;
using Snaptic.Core.Abstractions;
using Snaptic.Core.Barcodes;

namespace Snaptic.Core.Recognition;

/// <summary>
/// Facade mỏng cho decode + OCR. Cố ý KHÔNG tự ghép thành <see cref="CaptureAnalysis"/>:
/// hai phép chạy độc lập với tốc độ rất khác nhau (barcode ~10ms, OCR ~300ms) và tầng
/// ViewModel cần cập nhật nút dần theo từng kết quả về.
/// </summary>
public sealed class RecognitionService
{
    private readonly BarcodeDecoder _decoder;
    private readonly ITextRecognizer _ocr;

    public RecognitionService(BarcodeDecoder decoder, ITextRecognizer ocr)
    {
        _decoder = decoder;
        _ocr = ocr;
    }

    public bool IsOcrAvailable => _ocr.IsAvailable;

    public IReadOnlyList<string> GetAvailableLanguages() => _ocr.GetAvailableLanguages();

    public BarcodeResult? DecodeBarcode(SKBitmap image) => _decoder.Decode(image);

    /// <summary>
    /// Trả null khi: OCR không khả dụng, chưa chọn ngôn ngữ, hoặc engine ném lỗi.
    /// KHÔNG ném — OCR hỏng chỉ là không hiện nút, không phải sập luồng chụp.
    ///
    /// NGOẠI LỆ: OperationCanceledException được ném tiếp. Huỷ là tín hiệu điều khiển
    /// chứ không phải lỗi OCR — nuốt nó thì nơi gọi không biết tác vụ đã dừng và sẽ
    /// giải phóng ảnh khi OCR còn đang đọc dở.
    /// </summary>
    public async Task<string?> RecognizeTextAsync(
        SKBitmap image, string? languageTag, CancellationToken ct = default)
    {
        if (!_ocr.IsAvailable || string.IsNullOrWhiteSpace(languageTag))
            return null;

        try
        {
            return await _ocr.RecognizeAsync(image, languageTag, ct);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch
        {
            return null;
        }
    }
}
