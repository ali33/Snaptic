using SkiaSharp;

namespace Snaptic.Core.Images;

/// <summary>Encode ảnh sang PNG và sang data URI để nhúng vào HTML/CSS/Markdown.</summary>
public static class DataUriEncoder
{
    private const string PngDataUriPrefix = "data:image/png;base64,";

    public static byte[] ToPngBytes(SKBitmap bitmap)
    {
        using var image = SKImage.FromBitmap(bitmap);
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        return data.ToArray();
    }

    public static string ToPngDataUri(SKBitmap bitmap)
        => PngDataUriPrefix + Convert.ToBase64String(ToPngBytes(bitmap));
}
