using Snaptic.Core.Recognition;

namespace Snaptic.Core.Tests;

public class OcrLanguagePickerTests
{
    [Fact]
    public void Chon_ngon_ngu_he_thong_khi_may_doc_duoc()
        => Assert.Equal("ja-JP", OcrLanguagePicker.Pick(["en-US", "ja-JP"], "ja-JP"));

    [Fact]
    public void Lui_ve_tieng_Anh_khi_ngon_ngu_he_thong_khong_co()
    {
        // ĐÂY CHÍNH LÀ CA CỦA NGƯỜI DÙNG: hệ thống vi-VN, mà Windows không phát hành
        // gói OCR tiếng Việt (spec §8 R1). Phải tự lùi về tiếng Anh chứ không được
        // trả null — trả null là OCR im lặng không chạy.
        Assert.Equal("en-US", OcrLanguagePicker.Pick(["en-US", "ja-JP"], "vi-VN"));
    }

    [Fact]
    public void Khop_tieng_Anh_theo_tien_to_khong_can_dung_het_the()
        => Assert.Equal("en-GB", OcrLanguagePicker.Pick(["en-GB", "ja-JP"], "vi-VN"));

    [Fact]
    public void Khop_ngon_ngu_he_thong_theo_tien_to_khi_khong_dung_het_the()
    {
        // Hệ thống en-AU, máy chỉ có en-US: vẫn nên chọn en-US chứ không phải ja-JP.
        Assert.Equal("en-US", OcrLanguagePicker.Pick(["ja-JP", "en-US"], "en-AU"));
    }

    [Fact]
    public void Lay_cai_dau_tien_khi_khong_co_ca_tieng_Anh()
        => Assert.Equal("ja-JP", OcrLanguagePicker.Pick(["ja-JP", "ko-KR"], "vi-VN"));

    [Fact]
    public void Tra_null_khi_may_khong_co_ngon_ngu_nao()
        => Assert.Null(OcrLanguagePicker.Pick([], "vi-VN"));

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void The_ngon_ngu_he_thong_rong_thi_van_lui_ve_tieng_Anh(string? systemTag)
        => Assert.Equal("en-US", OcrLanguagePicker.Pick(["ja-JP", "en-US"], systemTag!));

    [Fact]
    public void Khong_phan_biet_hoa_thuong()
        => Assert.Equal("en-US", OcrLanguagePicker.Pick(["en-US"], "EN-us"));
}
