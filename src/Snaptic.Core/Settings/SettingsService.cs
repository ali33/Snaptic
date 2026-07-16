using System.Text.Json;

namespace Snaptic.Core.Settings;

/// <summary>
/// Đọc/ghi config JSON. Nguyên tắc: file hỏng hoặc thiếu KHÔNG BAO GIỜ được làm crash app
/// — luôn lùi về mặc định. Config là thứ người dùng sửa tay được, phải chịu được rác.
/// </summary>
public sealed class SettingsService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    private readonly string _configPath;

    public SettingsService(string configPath) => _configPath = configPath;

    public static string DefaultConfigPath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "Snaptic",
        "config.json");

    public static string DefaultSaveFolder => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.MyPictures),
        "Snaptic");

    /// <summary>
    /// Chưa từng lưu config lần nào — tức lần chạy đầu tiên trên máy này.
    /// Dùng để áp mặc định chỉ-một-lần (vd bật khởi động cùng Windows). Sau khi người
    /// dùng đã tự tắt đi thì KHÔNG được bật lại, nên luật đó chỉ chạy khi cờ này đúng.
    /// </summary>
    public bool IsFirstRun => !File.Exists(_configPath);

    public AppSettings Load()
    {
        try
        {
            if (!File.Exists(_configPath))
                return new AppSettings();

            var json = File.ReadAllText(_configPath);
            if (string.IsNullOrWhiteSpace(json))
                return new AppSettings();

            var settings = JsonSerializer.Deserialize<AppSettings>(json, JsonOptions);
            if (settings is null)
                return new AppSettings();

            // Config sửa tay được nên phải tự vệ: phím tắt không hợp lệ → về mặc định.
            return settings.Hotkey.IsValid
                ? settings
                : settings with { Hotkey = HotkeyCombo.Default };
        }
        catch (Exception ex) when (ex is JsonException or IOException or UnauthorizedAccessException)
        {
            return new AppSettings();
        }
    }

    public void Save(AppSettings settings)
    {
        var dir = Path.GetDirectoryName(_configPath);
        if (!string.IsNullOrEmpty(dir))
            Directory.CreateDirectory(dir);

        File.WriteAllText(_configPath, JsonSerializer.Serialize(settings, JsonOptions));
    }
}
