using SkiaSharp;
using Snaptic.Core.Abstractions;
using Snaptic.Core.Images;
using Windows.Globalization;
using Windows.Graphics.Imaging;
using Windows.Media.Ocr;
using Windows.Storage.Streams;

namespace Snaptic.Windows;

/// <summary>
/// OCR bằng engine sẵn trong Windows. Miễn phí, chạy offline, không tải model.
///
/// GIỚI HẠN ĐÃ BIẾT: Windows KHÔNG có gói OCR tiếng Việt (đã kiểm chứng — spec §8 R1).
/// Máy chỉ đọc được ngôn ngữ đã cài gói. Đây là lý do GetAvailableLanguages() tồn tại:
/// app nói thật máy đọc được gì thay vì hứa suông.
/// </summary>
public sealed class WindowsOcr : ITextRecognizer
{
    public bool IsAvailable => OcrEngine.AvailableRecognizerLanguages.Count > 0;

    public IReadOnlyList<string> GetAvailableLanguages()
        => OcrEngine.AvailableRecognizerLanguages
            .Select(l => l.LanguageTag)
            .ToList();

    public async Task<string?> RecognizeAsync(
        SKBitmap image, string languageTag, CancellationToken ct = default)
    {
        var engine = TryCreateEngine(languageTag);
        if (engine is null)
            return null;   // gói ngôn ngữ này không có trên máy

        using var softwareBitmap = await ToSoftwareBitmapAsync(image, ct);
        var result = await engine.RecognizeAsync(softwareBitmap).AsTask(ct);

        return string.IsNullOrWhiteSpace(result.Text) ? null : result.Text;
    }

    /// <summary>
    /// Trả null thay vì ném khi thẻ ngôn ngữ không hợp lệ. Thẻ này đến từ file config
    /// mà người dùng sửa tay được, nên rác là chuyện bình thường — không phải lý do crash.
    /// </summary>
    private static OcrEngine? TryCreateEngine(string languageTag)
    {
        try
        {
            return OcrEngine.TryCreateFromLanguage(new Language(languageTag));
        }
        catch (ArgumentException)
        {
            return null;
        }
    }

    /// <summary>
    /// SKBitmap → SoftwareBitmap. Đi vòng qua PNG vì đó là đường ngắn nhất mà không
    /// phải tự quản bộ nhớ liên tầng. Ảnh chụp một vùng màn hình nên chi phí không đáng kể.
    /// </summary>
    private static async Task<SoftwareBitmap> ToSoftwareBitmapAsync(
        SKBitmap image, CancellationToken ct)
    {
        var png = DataUriEncoder.ToPngBytes(image);

        using var stream = new InMemoryRandomAccessStream();
        using (var writer = new DataWriter(stream))
        {
            writer.WriteBytes(png);
            await writer.StoreAsync().AsTask(ct);
            await writer.FlushAsync().AsTask(ct);
            writer.DetachStream();
        }

        stream.Seek(0);
        var decoder = await BitmapDecoder.CreateAsync(stream).AsTask(ct);
        return await decoder.GetSoftwareBitmapAsync().AsTask(ct);
    }
}
