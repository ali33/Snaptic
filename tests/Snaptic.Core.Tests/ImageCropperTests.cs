using SkiaSharp;
using Snaptic.Core.Barcodes;
using Snaptic.Core.Display;
using Snaptic.Core.Images;

namespace Snaptic.Core.Tests;

public class ImageCropperTests
{
    /// <summary>Ảnh mô phỏng màn hình: nền trắng, có một QR đặt ở giữa.</summary>
    private static SKBitmap ScreenWithQr(string text, int screenW, int screenH, int qrX, int qrY)
    {
        var screen = new SKBitmap(new SKImageInfo(screenW, screenH, SKColorType.Bgra8888, SKAlphaType.Opaque));
        using var canvas = new SKCanvas(screen);
        canvas.Clear(SKColors.White);
        using var qr = BarcodeGenerator.Generate(SnapticFormat.Qr, text);
        canvas.DrawBitmap(qr, qrX, qrY);
        return screen;
    }

    [Fact]
    public void Anh_cat_ra_phai_lien_mach_khong_dung_chung_stride_voi_anh_goc()
    {
        // ĐÂY LÀ BUG THẬT ĐÃ XẢY RA: SKBitmap.ExtractSubset tạo bitmap DÙNG CHUNG bộ
        // nhớ với ảnh gốc, nên RowBytes vẫn là stride của ảnh GỐC chứ không phải
        // Width*4. Thư viện nào đọc buffer như dữ liệu liền mạch (ZXing) sẽ thấy ảnh
        // bị xé chéo. Tệ hơn: encode PNG thì vẫn ra ảnh ĐÚNG, nên nhìn mắt không thấy gì.
        using var screen = ScreenWithQr("x", 800, 600, 100, 100);
        using var cropped = ImageCropper.Crop(screen, new PhysicalRect(50, 50, 500, 500), 0, 0);

        Assert.Equal(cropped.Width * 4, cropped.RowBytes);
    }

    [Fact]
    public void Cat_vung_chua_QR_thi_van_decode_duoc()
    {
        // Test then chốt: nếu ai đó đổi lại sang ExtractSubset, test này đỏ.
        const string payload = "https://example.com/test";
        using var screen = ScreenWithQr(payload, 900, 700, 150, 100);

        using var cropped = ImageCropper.Crop(screen, new PhysicalRect(120, 70, 480, 480), 0, 0);
        var result = new BarcodeDecoder().Decode(cropped);

        Assert.NotNull(result);
        Assert.Equal(payload, result!.Text);
    }

    [Fact]
    public void Tru_goc_desktop_ao_khi_man_phu_nam_ben_trai()
    {
        // Desktop ảo có gốc ÂM khi màn phụ nằm bên trái màn chính. Vùng chọn dùng toạ
        // độ toàn cục, còn ảnh đóng băng đánh số từ 0 — phải trừ gốc đi.
        using var screen = ScreenWithQr("y", 400, 400, 0, 0);

        using var cropped = ImageCropper.Crop(screen, new PhysicalRect(-1920 + 10, -50, 100, 100), -1920, -100);

        Assert.Equal(100, cropped.Width);
        Assert.Equal(100, cropped.Height);
    }

    [Theory]
    [InlineData(-500, -500, 100, 100)]      // hoàn toàn ngoài biên trái-trên
    [InlineData(9000, 9000, 100, 100)]      // hoàn toàn ngoài biên phải-dưới
    [InlineData(350, 350, 999, 999)]        // tràn ra ngoài biên phải-dưới
    public void Vung_tran_ra_ngoai_bien_bi_kep_lai_khong_nem(int x, int y, int w, int h)
    {
        using var screen = ScreenWithQr("z", 400, 400, 0, 0);

        using var cropped = ImageCropper.Crop(screen, new PhysicalRect(x, y, w, h), 0, 0);

        Assert.True(cropped.Width >= 1);
        Assert.True(cropped.Height >= 1);
        Assert.True(cropped.Width <= 400);
        Assert.True(cropped.Height <= 400);
    }

    [Fact]
    public void Giu_dung_dinh_dang_mau_cua_anh_goc()
    {
        using var screen = ScreenWithQr("w", 300, 300, 0, 0);
        using var cropped = ImageCropper.Crop(screen, new PhysicalRect(10, 10, 100, 100), 0, 0);

        Assert.Equal(SKColorType.Bgra8888, cropped.ColorType);
    }
}
