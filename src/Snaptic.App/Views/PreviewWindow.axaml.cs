using System.ComponentModel;
using System.Diagnostics;
using Avalonia.Controls;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using SkiaSharp;
using Snaptic.App.ViewModels;

namespace Snaptic.App.Views;

public partial class PreviewWindow : Window
{
    private readonly PreviewViewModel? _vm;
    private readonly CancellationTokenSource _recognitionCts = new();
    private Task _recognitionTask = Task.CompletedTask;

    /// <summary>Ctor rỗng cho XAML designer — không dùng lúc chạy.</summary>
    public PreviewWindow() : this(null) { }

    public PreviewWindow(PreviewViewModel? vm)
    {
        InitializeComponent();
        _vm = vm;
        if (vm is null)
            return;

        DataContext = vm;
        PreviewImage.Source = ToAvaloniaBitmap(vm.Image);

        CopyImageButton.Click += (_, _) => Guard(vm.CopyImage);
        CopyDataUriButton.Click += (_, _) => Guard(vm.CopyDataUri);
        CopyCodeButton.Click += (_, _) => Guard(vm.CopyCode);
        CopyOcrButton.Click += (_, _) => Guard(vm.CopyOcr);
        SaveButton.Click += async (_, _) => await SaveAsync();
        OpenLinkButton.Click += (_, _) => OpenLink();

        vm.PropertyChanged += OnVmPropertyChanged;
        SyncButtons();

        // Cửa sổ SỞ HỮU ảnh — không giải phóng là rò vài chục MB mỗi lần chụp.
        //
        // THỨ TỰ QUAN TRỌNG: nhận diện chạy nền SAU khi cửa sổ đã hiện. Đóng cửa sổ
        // trong lúc OCR đang chạy mà giải phóng ảnh ngay là rút ảnh khỏi dưới chân OCR
        // → crash. Phải huỷ trước, chờ dừng hẳn, rồi mới giải phóng.
        Closed += async (_, _) =>
        {
            vm.PropertyChanged -= OnVmPropertyChanged;

            await _recognitionCts.CancelAsync();
            try { await _recognitionTask; }
            catch (OperationCanceledException) { /* đúng như mong đợi */ }
            catch { /* lỗi nhận diện đã xử lý ở VM; ở đây chỉ cần nó dừng */ }

            _recognitionCts.Dispose();
            vm.Image.Dispose();
        };
    }

    /// <summary>
    /// Gọi SAU khi Show(). Cửa sổ giữ Task để lúc đóng còn chờ nó dừng hẳn
    /// trước khi giải phóng ảnh.
    /// </summary>
    public void StartRecognition()
    {
        if (_vm is not null)
            _recognitionTask = _vm.RunRecognitionAsync(_recognitionCts.Token);
    }

    private static Bitmap ToAvaloniaBitmap(SKBitmap skBitmap)
    {
        using var image = SKImage.FromBitmap(skBitmap);
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        using var stream = new MemoryStream(data.ToArray());
        return new Bitmap(stream);
    }

    private void OnVmPropertyChanged(object? sender, PropertyChangedEventArgs e)
        => Dispatcher.UIThread.Post(SyncButtons);

    /// <summary>Nút mọc dần khi kết quả nhận diện về.</summary>
    private void SyncButtons()
    {
        if (_vm is null)
            return;

        CopyCodeButton.IsVisible = _vm.ShowCopyCode;
        CopyCodeButton.Content = _vm.CopyCodeLabel;

        OpenLinkButton.IsVisible = _vm.ShowOpenLink;
        OpenLinkButton.Content = _vm.OpenLinkLabel;

        CopyOcrButton.IsVisible = _vm.ShowCopyOcr;
        CopyOcrButton.Content = _vm.CopyOcrLabel;
    }

    /// <summary>Clipboard bị app khác giữ là lỗi có thật — không được để sập cửa sổ.</summary>
    private void Guard(Action action)
    {
        try
        {
            action();
        }
        catch (InvalidOperationException ex)
        {
            ToastWindow.Show(this, ex.Message);
        }
    }

    private async Task SaveAsync()
    {
        if (_vm is null)
            return;

        Directory.CreateDirectory(_vm.DefaultSaveFolder);
        var startFolder = await StorageProvider.TryGetFolderFromPathAsync(_vm.DefaultSaveFolder);

        var file = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Lưu ảnh",
            SuggestedFileName = _vm.SuggestedFileName,
            SuggestedStartLocation = startFolder,
            DefaultExtension = "png",
            FileTypeChoices = [new FilePickerFileType("Ảnh PNG") { Patterns = ["*.png"] }]
        });

        if (file is null)
            return;   // người dùng bấm Cancel

        try
        {
            await using var stream = await file.OpenWriteAsync();
            await stream.WriteAsync(_vm.ToPngBytes());
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            ToastWindow.Show(this, $"Không lưu được file: {ex.Message}");
        }
    }

    private void OpenLink()
    {
        // An toàn: LinkToOpen chỉ trả giá trị khi LinkValidator đã xác nhận http/https.
        if (_vm?.LinkToOpen is not { } url)
            return;

        Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
    }
}
