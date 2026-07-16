using Snaptic.Core.Barcodes;

namespace Snaptic.Core.Tests;

public class BarcodeFormatSpecTests
{
    [Theory]
    [InlineData("hello")]
    [InlineData("https://example.com")]
    [InlineData("Tiếng Việt có dấu")]
    [InlineData("日本語")]
    public void Qr_nhan_moi_text(string text)
        => Assert.True(BarcodeFormatSpec.Validate(SnapticFormat.Qr, text).IsValid);

    [Theory]
    [InlineData(SnapticFormat.Qr)]
    [InlineData(SnapticFormat.Code128)]
    [InlineData(SnapticFormat.Ean13)]
    [InlineData(SnapticFormat.UpcA)]
    [InlineData(SnapticFormat.Code39)]
    public void Moi_dinh_dang_deu_tu_choi_text_rong(SnapticFormat format)
    {
        Assert.False(BarcodeFormatSpec.Validate(format, null).IsValid);
        Assert.False(BarcodeFormatSpec.Validate(format, "").IsValid);
        Assert.False(BarcodeFormatSpec.Validate(format, "   ").IsValid);
    }

    [Theory]
    [InlineData("ABC-123")]
    [InlineData("hello world")]
    [InlineData("!@#$%^&*()")]
    public void Code128_nhan_ascii(string text)
        => Assert.True(BarcodeFormatSpec.Validate(SnapticFormat.Code128, text).IsValid);

    [Fact]
    public void Code128_tu_choi_ky_tu_co_dau()
    {
        var r = BarcodeFormatSpec.Validate(SnapticFormat.Code128, "Tiếng Việt");
        Assert.False(r.IsValid);
        Assert.Contains("có dấu", r.Hint);
    }

    [Fact]
    public void Ean13_nhan_dung_12_chu_so()
        => Assert.True(BarcodeFormatSpec.Validate(SnapticFormat.Ean13, "123456789012").IsValid);

    [Theory]
    [InlineData("12345678901", 11)]      // thiếu 1
    [InlineData("1234567890123", 13)]    // thừa 1 — 13 số KHÔNG được nhận
    [InlineData("123456789", 9)]
    public void Ean13_tu_choi_sai_do_dai_va_bao_so_dang_co(string text, int actual)
    {
        var r = BarcodeFormatSpec.Validate(SnapticFormat.Ean13, text);
        Assert.False(r.IsValid);
        Assert.Contains("12 chữ số", r.Hint);
        Assert.Contains(actual.ToString(), r.Hint);
    }

    [Fact]
    public void Ean13_tu_choi_chu_cai()
    {
        var r = BarcodeFormatSpec.Validate(SnapticFormat.Ean13, "12345678901a");
        Assert.False(r.IsValid);
        Assert.Contains("chữ số", r.Hint);
    }

    [Fact]
    public void UpcA_nhan_dung_11_chu_so()
        => Assert.True(BarcodeFormatSpec.Validate(SnapticFormat.UpcA, "12345678901").IsValid);

    [Theory]
    [InlineData("1234567890")]     // 10
    [InlineData("123456789012")]   // 12
    public void UpcA_tu_choi_sai_do_dai(string text)
    {
        var r = BarcodeFormatSpec.Validate(SnapticFormat.UpcA, text);
        Assert.False(r.IsValid);
        Assert.Contains("11 chữ số", r.Hint);
    }

    [Theory]
    [InlineData("HELLO")]
    [InlineData("ABC-123")]
    [InlineData("A B C")]
    [InlineData("100$/50%")]
    [InlineData("A+B.C")]
    public void Code39_nhan_hoa_so_va_ky_tu_cho_phep(string text)
        => Assert.True(BarcodeFormatSpec.Validate(SnapticFormat.Code39, text).IsValid);

    [Fact]
    public void Code39_tu_choi_chu_thuong()
    {
        var r = BarcodeFormatSpec.Validate(SnapticFormat.Code39, "hello");
        Assert.False(r.IsValid);
        Assert.Contains("chữ thường", r.Hint);
    }

    [Fact]
    public void Code39_tu_choi_ky_tu_ngoai_bang()
    {
        var r = BarcodeFormatSpec.Validate(SnapticFormat.Code39, "ABC@123");
        Assert.False(r.IsValid);
        Assert.NotNull(r.Hint);
    }

    [Theory]
    [InlineData("A\0B")]      // NUL giữa chuỗi
    [InlineData("\0")]        // chỉ NUL
    [InlineData("ABC\0")]     // NUL cuối chuỗi
    public void Code39_tu_choi_ky_tu_NUL(string text)
    {
        // Bẫy va chạm sentinel: '\0' CHÍNH LÀ default(char). Nếu dùng
        // `FirstOrDefault(...) != default` để dò ký tự lạ thì NUL bị nhầm thành
        // "không tìm thấy" và lọt qua như hợp lệ.
        var r = BarcodeFormatSpec.Validate(SnapticFormat.Code39, text);
        Assert.False(r.IsValid);
        Assert.NotNull(r.Hint);
    }

    [Fact]
    public void Hint_rong_khi_hop_le()
    {
        var r = BarcodeFormatSpec.Validate(SnapticFormat.Qr, "ok");
        Assert.True(r.IsValid);
        Assert.Null(r.Hint);
    }
}
