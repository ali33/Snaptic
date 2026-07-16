using SkiaSharp;
using Snaptic.App.ViewModels;
using Snaptic.Core.Abstractions;
using Snaptic.Core.Links;

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
    public void Co_ban_quyen_va_giay_phep()
    {
        // GPL mục 15-16 yêu cầu chương trình tự thông báo giấy phép và miễn trừ
        // trách nhiệm cho người dùng, không chỉ nằm trong file LICENSE.
        var vm = new AboutViewModel(new FakeClipboard());

        Assert.Contains("2026", vm.Copyright);
        Assert.Contains("Nguyen Duc Son", vm.Copyright);
        Assert.Contains("GPL", vm.License);
        Assert.Contains("3", vm.License);
    }

    [Fact]
    public void Co_mien_tru_trach_nhiem()
    {
        var vm = new AboutViewModel(new FakeClipboard());

        Assert.Contains("KHÔNG", vm.Disclaimer);
        Assert.Contains("bảo đảm", vm.Disclaimer);
    }

    [Fact]
    public void Link_tai_ve_phai_la_http_hoac_https()
    {
        // KHÔNG phải nghi thức. Nút Tải về gọi Process.Start với UseShellExecute=true —
        // chuỗi không phải URL thì shell đem SHELL-EXECUTE nó như đường dẫn file. Sửa
        // hằng số này thành đường dẫn hay lệnh là biến nút vô hại thành nút chạy thứ
        // khác. Ràng bằng đúng LinkValidator dùng cho link QR.
        var vm = new AboutViewModel(new FakeClipboard());

        Assert.True(LinkValidator.IsOpenableUrl(vm.DownloadUrl));
    }

    [Fact]
    public void Phien_ban_khong_rong()
    {
        var vm = new AboutViewModel(new FakeClipboard());
        Assert.False(string.IsNullOrWhiteSpace(vm.Version));
    }
}
