using Snaptic.Core.Settings;

namespace Snaptic.Core.Tests;

public class HotkeyKeyTests
{
    [Theory]
    [InlineData("A", "A")]
    [InlineData("Z", "Z")]
    [InlineData("Q", "Q")]
    public void Chu_cai_giu_nguyen(string input, string expected)
        => Assert.Equal(expected, HotkeyKey.Normalize(input));

    [Theory]
    [InlineData("D0", "0")]
    [InlineData("D1", "1")]
    [InlineData("D9", "9")]
    public void Phim_so_cua_Avalonia_ten_la_D0_den_D9_phai_doi_ve_so(string input, string expected)
    {
        // ĐÂY LÀ BUG THẬT: Avalonia đặt tên enum Key cho phím số là D0..D9, không phải
        // 0..9. Bấm Ctrl+Alt+1 sinh ra "D1", tầng Windows không hiểu, trả về "không đăng
        // ký được" và app đổ lỗi cho một ứng dụng khác không hề tồn tại.
        Assert.Equal(expected, HotkeyKey.Normalize(input));
    }

    [Theory]
    [InlineData("F1", "F1")]
    [InlineData("F12", "F12")]
    [InlineData("F24", "F24")]
    public void Phim_chuc_nang_giu_nguyen(string input, string expected)
        => Assert.Equal(expected, HotkeyKey.Normalize(input));

    [Theory]
    [InlineData("NumPad0", "NumPad0")]
    [InlineData("NumPad9", "NumPad9")]
    [InlineData("Space", "Space")]
    [InlineData("Insert", "Insert")]
    [InlineData("Delete", "Delete")]
    [InlineData("Home", "Home")]
    [InlineData("End", "End")]
    [InlineData("PageUp", "PageUp")]
    [InlineData("PageDown", "PageDown")]
    [InlineData("Left", "Left")]
    [InlineData("Right", "Right")]
    [InlineData("Up", "Up")]
    [InlineData("Down", "Down")]
    [InlineData("PrintScreen", "PrintScreen")]
    public void Cac_phim_dat_ten_duoc_ho_tro(string input, string expected)
        => Assert.Equal(expected, HotkeyKey.Normalize(input));

    [Fact]
    public void Khong_phan_biet_hoa_thuong()
    {
        Assert.Equal("A", HotkeyKey.Normalize("a"));
        Assert.Equal("1", HotkeyKey.Normalize("d1"));
        Assert.Equal("F1", HotkeyKey.Normalize("f1"));
    }

    [Theory]
    [InlineData("Escape")]        // Windows giữ riêng, không cho đăng ký
    [InlineData("Tab")]
    [InlineData("Enter")]
    [InlineData("Back")]
    [InlineData("OemPlus")]       // tên enum thô, không nên lộ cho người dùng
    [InlineData("KhongCoThat")]
    [InlineData("F25")]           // Windows chỉ có tới F24
    [InlineData("F0")]
    [InlineData("D10")]
    [InlineData("")]
    [InlineData("  ")]
    [InlineData(null)]
    public void Phim_khong_ho_tro_tra_null(string? input)
        => Assert.Null(HotkeyKey.Normalize(input));

    [Fact]
    public void CanNormalize_hoi_co_doi_duoc_thanh_ten_chuan_khong()
    {
        Assert.True(HotkeyKey.CanNormalize("D1"));    // đổi được thành "1"
        Assert.True(HotkeyKey.CanNormalize("A"));
        Assert.False(HotkeyKey.CanNormalize("Escape"));
        Assert.False(HotkeyKey.CanNormalize(null));
    }

    [Fact]
    public void IsCanonical_hoi_DA_LA_ten_chuan_chua()
    {
        // Hai câu hỏi KHÁC NHAU: "D1" đổi được thành "1", nhưng bản thân nó không chuẩn.
        // Lẫn hai thứ này là config lưu "D1" và người dùng thấy "Ctrl+Alt+D1".
        Assert.False(HotkeyKey.IsCanonical("D1"));
        Assert.True(HotkeyKey.IsCanonical("1"));
        Assert.True(HotkeyKey.IsCanonical("A"));
        Assert.False(HotkeyKey.IsCanonical("a"));     // chữ thường chưa chuẩn
        Assert.False(HotkeyKey.IsCanonical(null));
    }

    [Fact]
    public void Ten_da_chuan_hoa_thi_chuan_hoa_lai_van_ra_chinh_no()
    {
        // Tầng Windows nhận tên ĐÃ chuẩn hoá; nếu chuẩn hoá không idempotent thì nó
        // sẽ vỡ khi đọc lại phím từ config.
        foreach (var name in new[] { "A", "1", "F5", "Space", "NumPad3", "PrintScreen" })
            Assert.Equal(name, HotkeyKey.Normalize(name));
    }
}
