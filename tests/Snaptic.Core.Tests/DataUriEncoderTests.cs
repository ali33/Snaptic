using SkiaSharp;
using Snaptic.Core.Images;

namespace Snaptic.Core.Tests;

public class DataUriEncoderTests
{
    private static SKBitmap MakeBitmap(int w = 4, int h = 4)
    {
        var bmp = new SKBitmap(w, h);
        using var canvas = new SKCanvas(bmp);
        canvas.Clear(SKColors.Red);
        return bmp;
    }

    [Fact]
    public void Co_dung_tien_to_data_uri()
    {
        using var bmp = MakeBitmap();
        var uri = DataUriEncoder.ToPngDataUri(bmp);
        Assert.StartsWith("data:image/png;base64,", uri);
    }

    [Fact]
    public void Phan_sau_tien_to_la_base64_hop_le()
    {
        using var bmp = MakeBitmap();
        var uri = DataUriEncoder.ToPngDataUri(bmp);
        var payload = uri["data:image/png;base64,".Length..];

        var bytes = Convert.FromBase64String(payload); // ném nếu không hợp lệ
        Assert.NotEmpty(bytes);
    }

    [Fact]
    public void Byte_dau_ra_dung_chu_ky_PNG()
    {
        using var bmp = MakeBitmap();
        var bytes = DataUriEncoder.ToPngBytes(bmp);

        // Chữ ký PNG: 89 50 4E 47 0D 0A 1A 0A
        Assert.Equal(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A },
                     bytes.Take(8).ToArray());
    }

    [Fact]
    public void Giai_ma_lai_ra_dung_kich_thuoc()
    {
        using var bmp = MakeBitmap(7, 3);
        var bytes = DataUriEncoder.ToPngBytes(bmp);

        using var decoded = SKBitmap.Decode(bytes);
        Assert.Equal(7, decoded.Width);
        Assert.Equal(3, decoded.Height);
    }
}
