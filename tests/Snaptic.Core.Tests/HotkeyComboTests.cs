using Snaptic.Core.Settings;

namespace Snaptic.Core.Tests;

public class HotkeyComboTests
{
    [Fact]
    public void Mac_dinh_la_Ctrl_Alt_Q()
    {
        Assert.Equal(HotkeyModifiers.Control | HotkeyModifiers.Alt, HotkeyCombo.Default.Modifiers);
        Assert.Equal("Q", HotkeyCombo.Default.Key);
        Assert.True(HotkeyCombo.Default.IsValid);
    }

    [Fact]
    public void Tu_choi_phim_tran_khong_co_phim_bo_tro()
    {
        // Gán phím trần 'A' thì mọi lần gõ chữ A ở bất cứ đâu đều bật overlay.
        var combo = new HotkeyCombo(HotkeyModifiers.None, "A");
        Assert.False(combo.IsValid);
    }

    [Theory]
    [InlineData(HotkeyModifiers.Control)]
    [InlineData(HotkeyModifiers.Alt)]
    [InlineData(HotkeyModifiers.Shift)]
    [InlineData(HotkeyModifiers.Win)]
    [InlineData(HotkeyModifiers.Control | HotkeyModifiers.Shift)]
    public void Chap_nhan_khi_co_it_nhat_mot_phim_bo_tro(HotkeyModifiers mods)
        => Assert.True(new HotkeyCombo(mods, "Q").IsValid);

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public void Tu_choi_khi_khong_co_phim_chinh(string? key)
        => Assert.False(new HotkeyCombo(HotkeyModifiers.Control, key!).IsValid);

    [Fact]
    public void ToString_hien_thi_doc_duoc()
        => Assert.Equal("Ctrl+Alt+Q", HotkeyCombo.Default.ToString());

    [Fact]
    public void ToString_day_du_moi_phim_bo_tro()
    {
        var combo = new HotkeyCombo(
            HotkeyModifiers.Control | HotkeyModifiers.Alt | HotkeyModifiers.Shift | HotkeyModifiers.Win,
            "F1");
        Assert.Equal("Ctrl+Alt+Shift+Win+F1", combo.ToString());
    }
}
