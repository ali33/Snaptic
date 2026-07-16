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

    [Theory]
    [InlineData("Xin chào các bạn, đây là một đoạn tiếng Việt khá dài để ép QR lên version cao hơn nhiều")]
    [InlineData("Đường Trần Hưng Đạo, Quận 1, Thành phố Hồ Chí Minh — https://example.com/rất/dài/lắm?q=xin+chào")]
    public void Qr_tieng_Viet_DAI_van_doc_lai_duoc(string text)
    {
        // Chữ Việt tốn nhiều byte hơn ASCII trong UTF-8 nên đẩy QR lên version cao.
        // Đây là chỗ mà phép đo module và lúc render PHẢI đồng ý với nhau: nếu đo
        // không truyền cùng hints thì ra số module khác (đo được 29 vs 33 cho một
        // chuỗi ngắn), và ta tính kích thước dựa trên số sai.
        using var bitmap = BarcodeGenerator.Generate(SnapticFormat.Qr, text);
        var result = _decoder.Decode(bitmap);

        Assert.NotNull(result);
        Assert.Equal(text, result!.Text);
    }

    [Fact]
    public void Code128_noi_dung_rat_dai_van_doc_lai_duoc()
    {
        // moduleCount vượt xa bề rộng mong muốn 400px → scale phải sàn về 1,
        // không được thành 0 (chia cho 0 hoặc ảnh rộng 0px).
        var longText = new string('A', 200);
        using var bitmap = BarcodeGenerator.Generate(SnapticFormat.Code128, longText);

        Assert.True(bitmap.Width > 400, $"ảnh phải rộng hơn 400px, đang {bitmap.Width}");
        Assert.Equal(longText, _decoder.Decode(bitmap)!.Text);
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
