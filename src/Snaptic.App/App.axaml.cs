using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Platform;
using Avalonia.Threading;
using Microsoft.Extensions.DependencyInjection;
using Snaptic.Core.Abstractions;
using Snaptic.Core.Barcodes;
using Snaptic.Core.Recognition;
using Snaptic.Core.Settings;

namespace Snaptic.App;

public partial class App : Application
{
    private IServiceProvider? _services;
    private TrayIcon? _trayIcon;
    private IHotkeyService? _hotkey;

    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            // App tray: không có cửa sổ chính. Đóng hết cửa sổ KHÔNG được thoát app,
            // chỉ menu Thoát mới được.
            desktop.ShutdownMode = ShutdownMode.OnExplicitShutdown;

            _services = ServiceRegistration.Build();
            var settingsService = _services.GetRequiredService<SettingsService>();
            var settings = settingsService.Load();

            ApplyFirstRunDefaults(settingsService, settings);
            SetupTray(desktop);
            SetupHotkey(settings.Hotkey);
        }

        base.OnFrameworkInitializationCompleted();
    }

    /// <summary>
    /// Mặc định chỉ áp MỘT LẦN, ở lần chạy đầu tiên trên máy.
    ///
    /// Khởi động cùng Windows mặc định BẬT. Nhưng chỉ ở lần đầu: người dùng tự tắt đi
    /// rồi mà lần sau app bật lại thì đó là app cãi lời chủ. Ghi config ngay để lần sau
    /// IsFirstRun thành false, kể cả khi họ chưa đổi cài đặt gì.
    /// </summary>
    private void ApplyFirstRunDefaults(SettingsService settingsService, AppSettings settings)
    {
        if (!settingsService.IsFirstRun)
            return;

        var startup = _services!.GetRequiredService<IStartupService>();
        // Chính sách hệ thống có thể khoá khoá Run. Không bật được thì thôi — đây là
        // mặc định tiện lợi, không phải tính năng người dùng yêu cầu, nên không đáng
        // làm phiền bằng thông báo lúc khởi động.
        startup.TrySetEnabled(true);

        settingsService.Save(settings);
    }

    private void SetupTray(IClassicDesktopStyleApplicationLifetime desktop)
    {
        var menu = new NativeMenu();

        var captureItem = new NativeMenuItem("Chụp màn hình");
        captureItem.Click += (_, _) => OnCaptureRequested();
        menu.Add(captureItem);

        var qrItem = new NativeMenuItem("Tạo QR...");
        qrItem.Click += (_, _) => OnGenerateRequested(SnapticFormat.Qr);
        menu.Add(qrItem);

        var barcodeItem = new NativeMenuItem("Tạo Barcode...");
        barcodeItem.Click += (_, _) => OnGenerateRequested(SnapticFormat.Code128);
        menu.Add(barcodeItem);

        menu.Add(new NativeMenuItemSeparator());

        var settingsItem = new NativeMenuItem("Cài đặt...");
        settingsItem.Click += (_, _) => OnSettingsRequested();
        menu.Add(settingsItem);

        var exitItem = new NativeMenuItem("Thoát");
        exitItem.Click += (_, _) => Shutdown(desktop);
        menu.Add(exitItem);

        _trayIcon = new TrayIcon
        {
            Icon = new WindowIcon(AssetLoader.Open(new Uri("avares://Snaptic.App/Assets/snaptic.ico"))),
            ToolTipText = "Snaptic",
            Menu = menu,
            IsVisible = true
        };
    }

    private void Shutdown(IClassicDesktopStyleApplicationLifetime desktop)
    {
        // Thứ tự quan trọng: nhả phím tắt và gỡ icon TRƯỚC khi tắt, nếu không icon
        // có thể còn nằm lại ở khay như một xác chết cho tới khi rê chuột qua.
        _hotkey?.Dispose();
        _trayIcon?.Dispose();
        _trayIcon = null;
        desktop.Shutdown();
    }

    private void SetupHotkey(HotkeyCombo combo)
    {
        _hotkey = _services!.GetRequiredService<IHotkeyService>();

        // Sự kiện này phát trên LUỒNG NỀN của WindowsHotkey — phải chuyển về luồng UI
        // trước khi đụng vào bất cứ thứ gì của Avalonia.
        _hotkey.Pressed += (_, _) => Dispatcher.UIThread.Post(OnCaptureRequested);

        if (_hotkey.TryRegister(combo))
            return;

        // Tổ hợp bị app khác chiếm. App VẪN CHẠY và vẫn chụp được từ menu tray —
        // phím tắt hỏng không phải lý do từ chối khởi động. Nhưng phải BÁO, vì im lặng
        // thì người dùng bấm phím tắt mãi không thấy gì mà chẳng hiểu tại sao.
        Dispatcher.UIThread.Post(() => Views.ToastWindow.ShowStandalone(
            $"Không đăng ký được phím tắt {combo} — có ứng dụng khác đang dùng. " +
            "Vẫn chụp được từ menu tray, hoặc đổi phím trong Cài đặt."));
    }

    /// <summary>
    /// Chờ thêm sau khi sự kiện Closed đã nổ, cho DWM vẽ xong khung hình không còn
    /// cửa sổ. Windows không có API nào báo "đã composite xong" nên đành chờ theo thời
    /// gian — nhưng chỉ sau khi Closed đã nổ, chứ không phải ngay sau Close().
    /// </summary>
    private const int CompositorSettleMs = 250;

    private bool _capturing;

    private async void OnCaptureRequested()
    {
        // Chặn bấm phím tắt chồng nhau: overlay thứ hai sẽ chụp trúng overlay thứ nhất.
        if (_capturing) return;
        _capturing = true;
        try
        {
            var coordinator = _services!.GetRequiredService<Services.CaptureCoordinator>();
            // KHÔNG using: quyền sở hữu ảnh chuyển sang PreviewWindow, nó dispose khi đóng.
            var result = await coordinator.CaptureRegionAsync();

            if (result is null)
                return;   // người dùng huỷ hoặc vùng quá nhỏ

            ShowPreview(result.Image, new Snaptic.Core.Recognition.CaptureAnalysis(), runRecognition: true);
        }
        catch (Exception ex)
        {
            // App là WinExe nên KHÔNG có console — ghi ra Console.Error là ném lỗi vào
            // hư không. Người dùng bấm phím tắt, không thấy gì, không hiểu tại sao.
            // Phải báo bằng thứ nhìn được.
            Views.ToastWindow.ShowStandalone($"Lỗi khi chụp: {ex.Message}");
        }
        finally
        {
            _capturing = false;
        }
    }


    /// <summary>
    /// Điền ngôn ngữ OCR mặc định khi người dùng chưa tự chọn.
    ///
    /// KHÔNG ghi xuống config: đây là suy luận lúc chạy, không phải lựa chọn của người
    /// dùng. Ghi xuống thì hôm nay chọn en-US, mai họ cài thêm gói tiếng Nhật, app vẫn
    /// bám en-US vì "đã có trong config rồi". Chỉ ghi khi họ tự chọn ở màn Cài đặt.
    /// </summary>
    private AppSettings ResolveOcrLanguage(AppSettings settings)
    {
        if (settings.OcrLanguage is not null)
            return settings;

        var recognizer = _services!.GetRequiredService<ITextRecognizer>();
        var picked = OcrLanguagePicker.Pick(
            recognizer.GetAvailableLanguages(), CultureInfo.CurrentUICulture.Name);

        return settings with { OcrLanguage = picked };
    }

    /// <summary>
    /// Mở cửa sổ preview. <paramref name="runRecognition"/> = false cho đường tạo mã —
    /// vừa tự gõ text ra mã thì đọc lại vô nghĩa.
    /// </summary>
    private void ShowPreview(
        SkiaSharp.SKBitmap image,
        Snaptic.Core.Recognition.CaptureAnalysis analysis,
        bool runRecognition)
    {
        var services = _services!;
        var settings = ResolveOcrLanguage(services.GetRequiredService<SettingsService>().Load());

        var vm = new ViewModels.PreviewViewModel(
            image,
            analysis,
            services.GetRequiredService<Snaptic.Core.Recognition.RecognitionService>(),
            services.GetRequiredService<IClipboardService>(),
            settings);

        var window = new Views.PreviewWindow(vm);

        window.RecaptureRequested += async () =>
        {
            // Đóng cửa sổ TRƯỚC khi chụp lại. App đóng băng màn hình khi chụp, nên
            // preview còn hiện là nó TỰ LỌT VÀO ảnh mới. Đã xảy ra thật khi thi công.
            //
            // Close() trả về NGAY, nhưng cửa sổ chưa biến mất: Avalonia huỷ nó ở nhịp
            // dispatcher sau, rồi DWM còn cần thêm vài khung hình nữa mới xoá khỏi màn
            // hình thật. Nên phải đợi ĐÚNG sự kiện Closed trước, sau đó mới chờ thêm
            // cho compositor bắt kịp. Chờ mù bằng Task.Delay ngay sau Close() là không
            // đủ — bản đầu dùng 150ms và preview vẫn lọt vào ảnh.
            var closed = new TaskCompletionSource();
            window.Closed += (_, _) => closed.TrySetResult();
            window.Close();
            await closed.Task;

            await Task.Delay(CompositorSettleMs);

            OnCaptureRequested();
        };

        window.Show();

        // Chạy nhận diện SAU khi cửa sổ đã hiện — không để OCR làm khựng.
        // Cửa sổ tự giữ Task và CancellationTokenSource để lúc đóng còn huỷ và chờ
        // dừng hẳn trước khi giải phóng ảnh.
        if (runRecognition)
            window.StartRecognition();
    }

    // Hai hàm dưới được nối vào ở Task 15, 16.
    private void OnGenerateRequested(SnapticFormat format)
    {
        var window = new Views.GeneratorWindow(format);

        // Analysis RỖNG: vừa tự gõ text ra mã thì đọc lại vô nghĩa. Đây là lý do
        // PreviewWindow nhận (ảnh, analysis) chứ không có cờ chế độ.
        window.CodeGenerated += bitmap =>
            ShowPreview(bitmap, CaptureAnalysis.Empty, runRecognition: false);

        window.Show();
    }
    private void OnSettingsRequested()
    {
        var services = _services!;
        var vm = new ViewModels.SettingsViewModel(
            services.GetRequiredService<SettingsService>(),
            services.GetRequiredService<ITextRecognizer>(),
            services.GetRequiredService<IHotkeyService>(),
            services.GetRequiredService<IStartupService>());

        new Views.SettingsWindow(vm).Show();
    }
}
