using SkiaSharp;
using Snaptic.App.ViewModels;
using Snaptic.Core.Abstractions;

namespace Snaptic.App.Tests;

public class AboutViewModelTests
{
    private sealed class FakeClipboard : IClipboardService
    {
        public string? LastText { get; private set; }
        public void SetImage(SKBitmap image) { }
        public void SetText(string text) => LastText = text;
    }

    [Fact]
    public void Co_loi_de_tang()
    {
        var vm = new AboutViewModel(new FakeClipboard());

        Assert.Contains("Vũ Diệu Huyền", vm.Dedication);
        Assert.Contains("UBND xã Đan Phượng", vm.Dedication);
    }

    [Fact]
    public void Co_thong_tin_tai_khoan()
    {
        var vm = new AboutViewModel(new FakeClipboard());

        Assert.Contains("Techcombank", vm.BankName);
        Assert.Contains("NGUYEN DUC SON", vm.AccountHolder);
        Assert.Equal("7030456789", vm.AccountNumber);
    }

    [Fact]
    public void Copy_so_tai_khoan_dat_SO_TRAN_vao_clipboard()
    {
        // Số phải copy được để dán thẳng vào app ngân hàng — không khoảng trắng,
        // không dấu gạch. Hiển thị thì nhóm cho dễ đọc, copy thì phải sạch.
        var clipboard = new FakeClipboard();
        var vm = new AboutViewModel(clipboard);

        vm.CopyAccountNumber();

        Assert.Equal("7030456789", clipboard.LastText);
        Assert.DoesNotContain(" ", clipboard.LastText);
    }

    [Fact]
    public void So_tai_khoan_hien_thi_duoc_nhom_cho_de_doc()
    {
        var vm = new AboutViewModel(new FakeClipboard());

        Assert.Equal("7030 4567 89", vm.AccountNumberDisplay);
    }

    [Fact]
    public void Phien_ban_khong_rong()
    {
        var vm = new AboutViewModel(new FakeClipboard());
        Assert.False(string.IsNullOrWhiteSpace(vm.Version));
    }
}
