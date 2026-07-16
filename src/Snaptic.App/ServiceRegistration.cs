using Microsoft.Extensions.DependencyInjection;
using Snaptic.App.Services;
using Snaptic.Core.Abstractions;
using Snaptic.Core.Barcodes;
using Snaptic.Core.Recognition;
using Snaptic.Core.Settings;
using Snaptic.Windows;

namespace Snaptic.App;

/// <summary>
/// ⚠ ĐÂY LÀ FILE DUY NHẤT TRONG Snaptic.App ĐƯỢC PHÉP `using Snaptic.Windows`.
///
/// Mọi file khác chỉ được dùng interface của Snaptic.Core. Gọi thẳng WindowsClipboard
/// ở chỗ khác cho nhanh là làm hỏng toàn bộ mục đích của kiến trúc này — và sẽ chỉ lộ ra
/// vào lúc thêm macOS, khi đã quá muộn.
///
/// Thêm macOS sau này: đổi bốn dòng đăng ký dưới đây theo điều kiện nền tảng, và
/// multi-target project App.
/// </summary>
public static class ServiceRegistration
{
    public static IServiceProvider Build()
    {
        var services = new ServiceCollection();

        // ---- Tầng platform: bốn dòng duy nhất dính Windows ----
        services.AddSingleton<IScreenCapture, WindowsScreenCapture>();
        services.AddSingleton<ITextRecognizer, WindowsOcr>();
        services.AddSingleton<IClipboardService, WindowsClipboard>();
        services.AddSingleton<IHotkeyService, WindowsHotkey>();

        // ---- Core ----
        services.AddSingleton<BarcodeDecoder>();
        services.AddSingleton<RecognitionService>();
        services.AddSingleton(_ => new SettingsService(SettingsService.DefaultConfigPath));

        // ---- App ----
        services.AddSingleton<CaptureCoordinator>();

        return services.BuildServiceProvider();
    }
}
