using Snaptic.Core.Barcodes;

namespace Snaptic.Core.Recognition;

/// <summary>
/// Kết quả phân tích một ảnh. Cửa sổ preview nhận (ảnh, CaptureAnalysis).
/// Đường tạo mã truyền <see cref="Empty"/> — không cần đọc lại thứ vừa tự gõ ra.
/// </summary>
public sealed record CaptureAnalysis
{
    public BarcodeResult? Barcode { get; init; }
    public RecognitionStatus BarcodeStatus { get; init; } = RecognitionStatus.Pending;
    public string? OcrText { get; init; }
    public RecognitionStatus OcrStatus { get; init; } = RecognitionStatus.Pending;

    /// <summary>
    /// Đã phân tích xong và không có gì — dùng cho ảnh mã tự tạo.
    /// Status là Done (không phải Pending) nên UI không hiện spinner chờ mãi.
    /// </summary>
    public static CaptureAnalysis Empty => new()
    {
        BarcodeStatus = RecognitionStatus.Done,
        OcrStatus = RecognitionStatus.Done
    };
}
