using SkiaSharp;
using Snaptic.App.ViewModels;
using Snaptic.Core.Abstractions;
using Snaptic.Core.Barcodes;
using Snaptic.Core.Recognition;
using Snaptic.Core.Settings;

namespace Snaptic.App.Tests;

public class PreviewViewModelTests
{
    private sealed class FakeClipboard : IClipboardService
    {
        public SKBitmap? LastImage { get; private set; }
        public string? LastText { get; private set; }
        public void SetImage(SKBitmap image) => LastImage = image;
        public void SetText(string text) => LastText = text;
    }

    private sealed class FakeOcr : ITextRecognizer
    {
        public bool IsAvailable { get; init; } = true;
        public string? Text { get; init; }
        public IReadOnlyList<string> GetAvailableLanguages() => ["en-US"];
        public Task<string?> RecognizeAsync(SKBitmap i, string l, CancellationToken ct = default)
            => Task.FromResult(Text);
    }

    private static SKBitmap Image() => new(10, 10);

    private static PreviewViewModel Build(
        CaptureAnalysis analysis, ITextRecognizer? ocr = null, IClipboardService? clipboard = null)
        => new(
            Image(),
            analysis,
            new RecognitionService(new BarcodeDecoder(), ocr ?? new FakeOcr()),
            clipboard ?? new FakeClipboard(),
            new AppSettings { OcrLanguage = "en-US" });

    // ---- Nút mã ----

    [Fact]
    public void Khong_hien_nut_ma_khi_dang_Pending()
    {
        var vm = Build(new CaptureAnalysis { BarcodeStatus = RecognitionStatus.Pending });
        Assert.False(vm.ShowCopyCode);
    }

    [Fact]
    public void Khong_hien_nut_ma_khi_Done_nhung_khong_tim_thay_ma()
    {
        var vm = Build(new CaptureAnalysis
        {
            BarcodeStatus = RecognitionStatus.Done,
            Barcode = null
        });
        Assert.False(vm.ShowCopyCode);
    }

    [Fact]
    public void Nhan_nut_la_Copy_QR_text_khi_ma_la_QR()
    {
        var vm = Build(new CaptureAnalysis
        {
            BarcodeStatus = RecognitionStatus.Done,
            Barcode = new BarcodeResult("QR_CODE", "hello", IsQr: true, IsHttpUrl: false)
        });

        Assert.True(vm.ShowCopyCode);
        Assert.Equal("Copy QR text", vm.CopyCodeLabel);
    }

    [Fact]
    public void Nhan_nut_la_Copy_barcode_khi_ma_la_1D()
    {
        var vm = Build(new CaptureAnalysis
        {
            BarcodeStatus = RecognitionStatus.Done,
            Barcode = new BarcodeResult("CODE_128", "ABC", IsQr: false, IsHttpUrl: false)
        });

        Assert.True(vm.ShowCopyCode);
        Assert.Equal("Copy barcode", vm.CopyCodeLabel);
    }

    // ---- Open link: hàng rào an toàn ----

    [Fact]
    public void Hien_Open_link_khi_ma_la_http_url()
    {
        var vm = Build(new CaptureAnalysis
        {
            BarcodeStatus = RecognitionStatus.Done,
            Barcode = new BarcodeResult("QR_CODE", "https://example.com", IsQr: true, IsHttpUrl: true)
        });

        Assert.True(vm.ShowOpenLink);
    }

    [Fact]
    public void KHONG_hien_Open_link_voi_scheme_nguy_hiem()
    {
        // IsHttpUrl=false vì LinkValidator đã chặn file:// ở tầng Core.
        var vm = Build(new CaptureAnalysis
        {
            BarcodeStatus = RecognitionStatus.Done,
            Barcode = new BarcodeResult("QR_CODE", "file:///C:/calc.exe", IsQr: true, IsHttpUrl: false)
        });

        Assert.False(vm.ShowOpenLink);
        Assert.True(vm.ShowCopyCode);   // text vẫn copy được
    }

    [Fact]
    public void LinkToOpen_tra_null_voi_scheme_nguy_hiem()
    {
        // Nút bị ẩn rồi, nhưng LinkToOpen là hàng rào thứ hai: nếu ai đó gọi thẳng
        // thì vẫn không được đưa scheme lạ cho Process.Start.
        var vm = Build(new CaptureAnalysis
        {
            BarcodeStatus = RecognitionStatus.Done,
            Barcode = new BarcodeResult("QR_CODE", "javascript:alert(1)", IsQr: true, IsHttpUrl: false)
        });

        Assert.Null(vm.LinkToOpen);
    }

    [Fact]
    public void Open_link_hien_URL_tren_nhan_de_thay_truoc_khi_bam()
    {
        var vm = Build(new CaptureAnalysis
        {
            BarcodeStatus = RecognitionStatus.Done,
            Barcode = new BarcodeResult("QR_CODE", "https://example.com", IsQr: true, IsHttpUrl: true)
        });

        Assert.Contains("https://example.com", vm.OpenLinkLabel);
    }

    // ---- Nút OCR ----

    [Theory]
    [InlineData(RecognitionStatus.Pending)]
    [InlineData(RecognitionStatus.Unavailable)]
    [InlineData(RecognitionStatus.Failed)]
    public void Khong_hien_nut_OCR_tru_khi_Done(RecognitionStatus status)
    {
        var vm = Build(new CaptureAnalysis { OcrStatus = status, OcrText = "có chữ" });
        Assert.False(vm.ShowCopyOcr);
    }

    [Fact]
    public void Hien_nut_OCR_khi_Done_va_co_chu()
    {
        var vm = Build(new CaptureAnalysis { OcrStatus = RecognitionStatus.Done, OcrText = "xin chào" });
        Assert.True(vm.ShowCopyOcr);
    }

    [Fact]
    public void Khong_hien_nut_OCR_khi_Done_nhung_khong_co_chu()
    {
        var vm = Build(new CaptureAnalysis { OcrStatus = RecognitionStatus.Done, OcrText = null });
        Assert.False(vm.ShowCopyOcr);
    }

    [Fact]
    public void Analysis_Empty_khong_hien_nut_theo_ngu_canh_nao()
    {
        // Đường tạo mã: vừa tự gõ text ra, đọc lại vô nghĩa.
        var vm = Build(CaptureAnalysis.Empty);

        Assert.False(vm.ShowCopyCode);
        Assert.False(vm.ShowOpenLink);
        Assert.False(vm.ShowCopyOcr);
    }

    // ---- Chạy nhận diện ----

    [Fact]
    public async Task RunRecognitionAsync_cap_nhat_nut_khi_ket_qua_ve()
    {
        using var qr = BarcodeGenerator.Generate(SnapticFormat.Qr, "https://example.com");
        var vm = new PreviewViewModel(
            qr,
            new CaptureAnalysis(),
            new RecognitionService(new BarcodeDecoder(), new FakeOcr { Text = "chữ đọc được" }),
            new FakeClipboard(),
            new AppSettings { OcrLanguage = "en-US" });

        Assert.False(vm.ShowCopyCode);   // trước khi chạy

        await vm.RunRecognitionAsync();

        Assert.True(vm.ShowCopyCode);
        Assert.True(vm.ShowOpenLink);
        Assert.True(vm.ShowCopyOcr);
    }

    [Fact]
    public async Task RunRecognitionAsync_danh_dau_Unavailable_khi_may_khong_co_OCR()
    {
        using var blank = new SKBitmap(50, 50);
        var vm = new PreviewViewModel(
            blank,
            new CaptureAnalysis(),
            new RecognitionService(new BarcodeDecoder(), new FakeOcr { IsAvailable = false }),
            new FakeClipboard(),
            new AppSettings());

        await vm.RunRecognitionAsync();

        Assert.False(vm.ShowCopyOcr);
    }

    // ---- Hành động ----

    [Fact]
    public void CopyImage_dat_anh_vao_clipboard()
    {
        var clipboard = new FakeClipboard();
        var vm = Build(CaptureAnalysis.Empty, clipboard: clipboard);

        vm.CopyImage();

        Assert.NotNull(clipboard.LastImage);
    }

    [Fact]
    public void CopyDataUri_dat_chuoi_data_uri_vao_clipboard()
    {
        var clipboard = new FakeClipboard();
        var vm = Build(CaptureAnalysis.Empty, clipboard: clipboard);

        vm.CopyDataUri();

        Assert.StartsWith("data:image/png;base64,", clipboard.LastText);
    }

    [Fact]
    public void CopyCode_dat_text_cua_ma_vao_clipboard()
    {
        var clipboard = new FakeClipboard();
        var vm = Build(new CaptureAnalysis
        {
            BarcodeStatus = RecognitionStatus.Done,
            Barcode = new BarcodeResult("QR_CODE", "nội dung mã", IsQr: true, IsHttpUrl: false)
        }, clipboard: clipboard);

        vm.CopyCode();

        Assert.Equal("nội dung mã", clipboard.LastText);
    }

    [Fact]
    public void Ten_file_goi_y_dung_dinh_dang_va_duoi_png()
    {
        var vm = Build(CaptureAnalysis.Empty);

        Assert.StartsWith("Snaptic_", vm.SuggestedFileName);
        Assert.EndsWith(".png", vm.SuggestedFileName);
    }
}
