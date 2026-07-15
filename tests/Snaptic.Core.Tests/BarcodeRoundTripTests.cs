using Snaptic.Core.Barcodes;

namespace Snaptic.Core.Tests;

public class BarcodeRoundTripTests
{
    private readonly BarcodeDecoder _decoder = new();

    [Theory]
    [InlineData(SnapticFormat.Qr, "hello")]
    [InlineData(SnapticFormat.Qr, "https://example.com/path?q=1")]
    [InlineData(SnapticFormat.Qr, "Tiếng Việt có dấu")]
    [InlineData(SnapticFormat.Code128, "ABC-123")]
    [InlineData(SnapticFormat.Code39, "HELLO-42")]
    public void Tao_roi_doc_lai_ra_dung_noi_dung(SnapticFormat format, string text)
    {
        using var bitmap = BarcodeGenerator.Generate(format, text);
        var result = _decoder.Decode(bitmap);

        Assert.NotNull(result);
        Assert.Equal(text, result!.Text);
    }

    [Fact]
    public void Ean13_tao_tu_12_so_doc_lai_ra_13_so_kem_so_kiem_tra()
    {
        using var bitmap = BarcodeGenerator.Generate(SnapticFormat.Ean13, "123456789012");
        var result = _decoder.Decode(bitmap);

        Assert.NotNull(result);
        Assert.Equal(13, result!.Text.Length);
        Assert.StartsWith("123456789012", result.Text);
    }

    [Fact]
    public void Qr_duoc_danh_dau_IsQr()
    {
        using var bitmap = BarcodeGenerator.Generate(SnapticFormat.Qr, "x");
        var result = _decoder.Decode(bitmap);

        Assert.NotNull(result);
        Assert.True(result!.IsQr);
    }

    [Fact]
    public void Barcode_1D_khong_duoc_danh_dau_IsQr()
    {
        using var bitmap = BarcodeGenerator.Generate(SnapticFormat.Code128, "ABC");
        var result = _decoder.Decode(bitmap);

        Assert.NotNull(result);
        Assert.False(result!.IsQr);
    }

    [Fact]
    public void Qr_chua_link_http_duoc_danh_dau_IsHttpUrl()
    {
        using var bitmap = BarcodeGenerator.Generate(SnapticFormat.Qr, "https://example.com");
        var result = _decoder.Decode(bitmap);

        Assert.NotNull(result);
        Assert.True(result!.IsHttpUrl);
    }

    [Fact]
    public void Qr_chua_scheme_nguy_hiem_KHONG_duoc_danh_dau_IsHttpUrl()
    {
        using var bitmap = BarcodeGenerator.Generate(
            SnapticFormat.Qr, "file:///C:/Windows/System32/calc.exe");
        var result = _decoder.Decode(bitmap);

        Assert.NotNull(result);
        Assert.False(result!.IsHttpUrl);
        Assert.Equal("file:///C:/Windows/System32/calc.exe", result.Text); // text vẫn copy được
    }

    [Fact]
    public void Qr_chua_text_thuong_khong_phai_url()
    {
        using var bitmap = BarcodeGenerator.Generate(SnapticFormat.Qr, "chỉ là ghi chú");
        var result = _decoder.Decode(bitmap);

        Assert.NotNull(result);
        Assert.False(result!.IsHttpUrl);
    }

    [Fact]
    public void Anh_khong_co_ma_tra_ve_null()
    {
        using var blank = new SkiaSharp.SKBitmap(200, 200);
        using (var canvas = new SkiaSharp.SKCanvas(blank))
            canvas.Clear(SkiaSharp.SKColors.White);

        Assert.Null(_decoder.Decode(blank));
    }

    [Fact]
    public void Generate_nem_khi_noi_dung_khong_hop_le_voi_dinh_dang()
    {
        // "hello" không phải 12 chữ số — BarcodeFormatSpec đã chặn ở UI,
        // nhưng Generate vẫn phải tự bảo vệ.
        Assert.Throws<ArgumentException>(
            () => BarcodeGenerator.Generate(SnapticFormat.Ean13, "hello"));
    }

    [Fact]
    public void Code128_voi_ky_tu_dieu_khien_ASCII_khong_lam_no_generator()
    {
        // Review Task 4 nêu: ValidateCode128 cho phép 0x00-0x1F vì đúng theo
        // symbology Code128 Set A. Nhưng "spec cho phép" khác "ZXing nuốt được".
        // Đây là chỗ kiểm điều đó — Validate nói OK thì Generate phải không nổ.
        const string withControl = "AB\tCD";
        Assert.True(BarcodeFormatSpec.Validate(SnapticFormat.Code128, withControl).IsValid);

        using var bitmap = BarcodeGenerator.Generate(SnapticFormat.Code128, withControl);
        Assert.NotNull(bitmap);
    }
}
