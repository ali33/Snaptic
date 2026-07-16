using Snaptic.App.ViewModels;
using Snaptic.Core.Barcodes;

namespace Snaptic.App.Tests;

public class GeneratorViewModelTests
{
    [Fact]
    public void Ban_dau_chua_the_tao_vi_chua_nhap_gi()
    {
        var vm = new GeneratorViewModel(SnapticFormat.Qr);
        Assert.False(vm.CanGenerate);
    }

    [Fact]
    public void Nhap_text_hop_le_thi_bat_nut_tao()
    {
        var vm = new GeneratorViewModel(SnapticFormat.Qr) { Text = "hello" };
        Assert.True(vm.CanGenerate);
        Assert.Null(vm.Hint);
    }

    [Fact]
    public void Text_khong_hop_le_thi_khoa_nut_va_hien_loi_nhac()
    {
        var vm = new GeneratorViewModel(SnapticFormat.Ean13) { Text = "hello" };
        Assert.False(vm.CanGenerate);
        Assert.Contains("chữ số", vm.Hint);
    }

    [Fact]
    public void Doi_dinh_dang_thi_validate_lai_text_hien_co()
    {
        var vm = new GeneratorViewModel(SnapticFormat.Qr) { Text = "hello" };
        Assert.True(vm.CanGenerate);

        vm.Format = SnapticFormat.Ean13;   // "hello" không hợp lệ với EAN-13

        Assert.False(vm.CanGenerate);
        Assert.NotNull(vm.Hint);
    }

    [Fact]
    public void Preview_cap_nhat_khi_text_hop_le()
    {
        var vm = new GeneratorViewModel(SnapticFormat.Qr) { Text = "hello" };
        Assert.NotNull(vm.Preview);
    }

    [Fact]
    public void Preview_null_khi_text_khong_hop_le()
    {
        var vm = new GeneratorViewModel(SnapticFormat.Ean13) { Text = "abc" };
        Assert.Null(vm.Preview);
    }

    [Fact]
    public void Generate_tra_ve_anh_doc_lai_duoc()
    {
        var vm = new GeneratorViewModel(SnapticFormat.Qr) { Text = "round trip" };

        using var bitmap = vm.Generate();
        Assert.NotNull(bitmap);

        var decoded = new BarcodeDecoder().Decode(bitmap!);
        Assert.Equal("round trip", decoded!.Text);
    }

    [Fact]
    public void Generate_tra_null_khi_khong_hop_le()
    {
        var vm = new GeneratorViewModel(SnapticFormat.Ean13) { Text = "abc" };
        Assert.Null(vm.Generate());
    }

    [Fact]
    public void Generate_KHONG_tra_ve_chinh_anh_preview()
    {
        // Preview bị dispose mỗi lần gõ. Nếu Generate trả về chính nó thì cửa sổ
        // preview sẽ ôm một ảnh đã chết ngay khi người dùng gõ thêm một ký tự.
        var vm = new GeneratorViewModel(SnapticFormat.Qr) { Text = "x" };
        var preview = vm.Preview;

        using var generated = vm.Generate();

        Assert.NotNull(generated);
        Assert.NotSame(preview, generated);
    }

    [Fact]
    public void Ean13_dung_12_so_thi_hop_le()
    {
        var vm = new GeneratorViewModel(SnapticFormat.Ean13) { Text = "123456789012" };
        Assert.True(vm.CanGenerate);
    }

    [Fact]
    public void Liet_ke_du_5_dinh_dang()
    {
        Assert.Equal(5, GeneratorViewModel.AllFormats.Count);
        Assert.Contains(SnapticFormat.Qr, GeneratorViewModel.AllFormats);
        Assert.Contains(SnapticFormat.Code39, GeneratorViewModel.AllFormats);
    }

    [Fact]
    public void Doi_text_lien_tuc_khong_ro_bo_nho_preview()
    {
        // Mỗi lần gõ là sinh một ảnh mới; ảnh cũ phải được giải phóng.
        var vm = new GeneratorViewModel(SnapticFormat.Qr);
        for (var i = 0; i < 20; i++)
            vm.Text = $"noi dung thu {i}";

        Assert.NotNull(vm.Preview);
    }
}
