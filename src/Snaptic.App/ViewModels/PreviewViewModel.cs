using System.ComponentModel;
using System.Runtime.CompilerServices;
using SkiaSharp;
using Snaptic.Core.Abstractions;
using Snaptic.Core.Images;
using Snaptic.Core.Recognition;
using Snaptic.Core.Settings;

namespace Snaptic.App.ViewModels;

/// <summary>
/// Logic hiện nút của cửa sổ preview. Nằm ở ViewModel chứ không trong XAML để test được
/// mà không cần bật cửa sổ — đây là toàn bộ lý do bỏ qua test UI tự động vẫn an toàn.
/// </summary>
public sealed class PreviewViewModel : INotifyPropertyChanged
{
    private readonly RecognitionService _recognition;
    private readonly IClipboardService _clipboard;
    private readonly AppSettings _settings;
    private CaptureAnalysis _analysis;

    public SKBitmap Image { get; }

    public PreviewViewModel(
        SKBitmap image,
        CaptureAnalysis analysis,
        RecognitionService recognition,
        IClipboardService clipboard,
        AppSettings settings)
    {
        Image = image;
        _analysis = analysis;
        _recognition = recognition;
        _clipboard = clipboard;
        _settings = settings;
    }

    // Nút Copy ảnh / Save / Copy Data URI KHÔNG có property ở đây: chúng luôn hiện,
    // nên XAML để hiện thẳng. Thêm property `=> true` rồi binding vào chỉ tạo ra thứ
    // trông như logic nhưng không phải, kèm test không bao giờ đỏ được.
    // Điều kiện "đủ nút ngay khi chưa nhận diện xong" được bảo đảm bằng cấu trúc.

    // ---- Nút theo ngữ cảnh ----

    public bool ShowCopyCode =>
        _analysis.BarcodeStatus == RecognitionStatus.Done && _analysis.Barcode is not null;

    public string CopyCodeLabel =>
        _analysis.Barcode?.IsQr == true ? "Copy QR text" : "Copy barcode";

    public bool ShowOpenLink =>
        ShowCopyCode && _analysis.Barcode!.IsHttpUrl;

    /// <summary>Hiện luôn URL để người dùng thấy mình sắp đi đâu TRƯỚC khi bấm.</summary>
    public string OpenLinkLabel =>
        _analysis.Barcode is null ? "Mở link" : $"Mở {_analysis.Barcode.Text}";

    public bool ShowCopyOcr =>
        _analysis.OcrStatus == RecognitionStatus.Done && !string.IsNullOrWhiteSpace(_analysis.OcrText);

    public string CopyOcrLabel =>
        $"Copy text ({_analysis.OcrText?.Length ?? 0} ký tự)";

    /// <summary>
    /// Hàng rào an toàn thứ hai. Nút đã bị ẩn khi không phải http/https, nhưng nếu ai đó
    /// gọi thẳng thì vẫn không được đưa scheme lạ cho Process.Start.
    /// </summary>
    public string? LinkToOpen =>
        _analysis.Barcode?.IsHttpUrl == true ? _analysis.Barcode.Text : null;

    /// <summary>
    /// Chạy decode và OCR. Cố ý KHÔNG chờ cả hai xong mới báo: barcode xong sau ~10ms
    /// còn OCR ~10-30ms, nút mọc dần chứ không đợi nhau.
    /// </summary>
    public async Task RunRecognitionAsync(CancellationToken ct = default)
    {
        var barcode = _recognition.DecodeBarcode(Image);
        _analysis = _analysis with
        {
            Barcode = barcode,
            BarcodeStatus = RecognitionStatus.Done
        };
        RaiseCodeButtons();

        if (!_recognition.IsOcrAvailable)
        {
            _analysis = _analysis with { OcrStatus = RecognitionStatus.Unavailable };
            RaiseOcrButtons();
            return;
        }

        try
        {
            var text = await _recognition.RecognizeTextAsync(Image, _settings.OcrLanguage, ct);
            _analysis = _analysis with { OcrText = text, OcrStatus = RecognitionStatus.Done };
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch
        {
            _analysis = _analysis with { OcrStatus = RecognitionStatus.Failed };
        }

        RaiseOcrButtons();
    }

    // ---- Hành động ----

    public void CopyImage() => _clipboard.SetImage(Image);

    public void CopyDataUri() => _clipboard.SetText(DataUriEncoder.ToPngDataUri(Image));

    public void CopyCode()
    {
        if (_analysis.Barcode is { } code)
            _clipboard.SetText(code.Text);
    }

    public void CopyOcr()
    {
        if (!string.IsNullOrWhiteSpace(_analysis.OcrText))
            _clipboard.SetText(_analysis.OcrText);
    }

    public string SuggestedFileName => $"Snaptic_{DateTime.Now:yyyy-MM-dd_HHmmss}.png";

    public string DefaultSaveFolder => _settings.DefaultSaveFolder;

    public byte[] ToPngBytes() => DataUriEncoder.ToPngBytes(Image);

    // ---- INotifyPropertyChanged ----

    public event PropertyChangedEventHandler? PropertyChanged;

    private void Raise([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

    private void RaiseCodeButtons()
    {
        Raise(nameof(ShowCopyCode));
        Raise(nameof(CopyCodeLabel));
        Raise(nameof(ShowOpenLink));
        Raise(nameof(OpenLinkLabel));
    }

    private void RaiseOcrButtons()
    {
        Raise(nameof(ShowCopyOcr));
        Raise(nameof(CopyOcrLabel));
    }
}
