using SkiaSharp;

namespace Snaptic.Core.Abstractions;

/// <summary>OCR. Hiện thực bởi tầng platform.</summary>
public interface ITextRecognizer
{
    /// <summary>False khi máy không có engine OCR nào — tầng UI phải ẩn nút OCR.</summary>
    bool IsAvailable { get; }

    /// <summary>Danh sách thẻ ngôn ngữ BCP-47 máy đọc được, ví dụ "en-US".</summary>
    IReadOnlyList<string> GetAvailableLanguages();

    /// <summary>Trả null khi không nhận ra chữ nào hoặc OCR không khả dụng.</summary>
    Task<string?> RecognizeAsync(SKBitmap image, string languageTag, CancellationToken ct = default);
}
