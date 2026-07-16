using SkiaSharp;
using Snaptic.Core.Barcodes;
using Snaptic.Core.Recognition;
using Snaptic.Core.Tests.Fakes;

namespace Snaptic.Core.Tests;

public class RecognitionServiceTests
{
    private static SKBitmap QrImage(string text) => BarcodeGenerator.Generate(SnapticFormat.Qr, text);

    private static SKBitmap BlankImage()
    {
        var bmp = new SKBitmap(100, 100);
        using var canvas = new SKCanvas(bmp);
        canvas.Clear(SKColors.White);
        return bmp;
    }

    [Fact]
    public void DecodeBarcode_tra_ve_ket_qua_khi_anh_co_ma()
    {
        var service = new RecognitionService(new BarcodeDecoder(), new FakeTextRecognizer());
        using var image = QrImage("hello");

        var result = service.DecodeBarcode(image);

        Assert.NotNull(result);
        Assert.Equal("hello", result!.Text);
    }

    [Fact]
    public void DecodeBarcode_tra_ve_null_khi_anh_khong_co_ma()
    {
        var service = new RecognitionService(new BarcodeDecoder(), new FakeTextRecognizer());
        using var image = BlankImage();

        Assert.Null(service.DecodeBarcode(image));
    }

    [Fact]
    public void IsOcrAvailable_phan_anh_recognizer()
    {
        var available = new RecognitionService(
            new BarcodeDecoder(), new FakeTextRecognizer { IsAvailable = true });
        var unavailable = new RecognitionService(
            new BarcodeDecoder(), new FakeTextRecognizer { IsAvailable = false });

        Assert.True(available.IsOcrAvailable);
        Assert.False(unavailable.IsOcrAvailable);
    }

    [Fact]
    public async Task RecognizeTextAsync_tra_ve_null_khi_OCR_khong_kha_dung()
    {
        var service = new RecognitionService(
            new BarcodeDecoder(), new FakeTextRecognizer { IsAvailable = false, TextToReturn = "xyz" });
        using var image = BlankImage();

        Assert.Null(await service.RecognizeTextAsync(image, "en-US"));
    }

    [Fact]
    public async Task RecognizeTextAsync_tra_ve_text_khi_OCR_chay_duoc()
    {
        var service = new RecognitionService(
            new BarcodeDecoder(), new FakeTextRecognizer { TextToReturn = "xin chào" });
        using var image = BlankImage();

        Assert.Equal("xin chào", await service.RecognizeTextAsync(image, "en-US"));
    }

    [Fact]
    public async Task RecognizeTextAsync_tra_ve_null_khi_chua_chon_ngon_ngu()
    {
        var service = new RecognitionService(
            new BarcodeDecoder(), new FakeTextRecognizer { TextToReturn = "xyz" });
        using var image = BlankImage();

        Assert.Null(await service.RecognizeTextAsync(image, null));
    }

    [Fact]
    public async Task RecognizeTextAsync_nuot_loi_va_tra_null_thay_vi_nem()
    {
        // OCR hỏng không được làm sập luồng chụp — chỉ là không hiện nút.
        var service = new RecognitionService(
            new BarcodeDecoder(),
            new FakeTextRecognizer { ThrowOnRecognize = new InvalidOperationException("engine chết") });
        using var image = BlankImage();

        Assert.Null(await service.RecognizeTextAsync(image, "en-US"));
    }

    [Fact]
    public async Task RecognizeTextAsync_KHONG_nuot_OperationCanceledException()
    {
        // Huỷ là tín hiệu điều khiển, không phải lỗi OCR. Nuốt nó thì cửa sổ preview
        // không biết tác vụ đã dừng và sẽ giải phóng ảnh khi OCR còn đang chạy.
        var service = new RecognitionService(
            new BarcodeDecoder(),
            new FakeTextRecognizer { ThrowOnRecognize = new OperationCanceledException() });
        using var image = BlankImage();

        await Assert.ThrowsAsync<OperationCanceledException>(
            () => service.RecognizeTextAsync(image, "en-US"));
    }

    [Fact]
    public async Task RecognizeTextAsync_truyen_dung_ngon_ngu_xuong_recognizer()
    {
        var fake = new FakeTextRecognizer { TextToReturn = "x" };
        var service = new RecognitionService(new BarcodeDecoder(), fake);
        using var image = BlankImage();

        await service.RecognizeTextAsync(image, "ja-JP");

        Assert.Equal("ja-JP", fake.LastLanguageUsed);
    }

    [Fact]
    public void GetAvailableLanguages_uy_quyen_cho_recognizer()
    {
        var service = new RecognitionService(
            new BarcodeDecoder(), new FakeTextRecognizer { Languages = ["en-US", "ja-JP"] });

        Assert.Equal(["en-US", "ja-JP"], service.GetAvailableLanguages());
    }
}
