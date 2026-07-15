namespace Snaptic.Core.Settings;

/// <summary>
/// Cấu hình app. <see cref="OcrLanguage"/> null nghĩa là chưa chọn — tầng UI sẽ
/// chọn mặc định theo danh sách ngôn ngữ OS trả về lúc chạy.
/// </summary>
public sealed record AppSettings
{
    public HotkeyCombo Hotkey { get; init; } = HotkeyCombo.Default;
    public string? OcrLanguage { get; init; }
    public string DefaultSaveFolder { get; init; } = SettingsService.DefaultSaveFolder;
}
