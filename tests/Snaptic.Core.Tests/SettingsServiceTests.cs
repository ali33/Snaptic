using Snaptic.Core.Settings;

namespace Snaptic.Core.Tests;

public class SettingsServiceTests : IDisposable
{
    private readonly string _dir;
    private readonly string _path;

    public SettingsServiceTests()
    {
        _dir = Path.Combine(Path.GetTempPath(), "snaptic-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_dir);
        _path = Path.Combine(_dir, "config.json");
    }

    public void Dispose()
    {
        if (Directory.Exists(_dir)) Directory.Delete(_dir, recursive: true);
    }

    [Fact]
    public void IsFirstRun_dung_khi_chua_co_file_config()
    {
        Assert.True(new SettingsService(_path).IsFirstRun);
    }

    [Fact]
    public void IsFirstRun_sai_sau_khi_da_luu()
    {
        var service = new SettingsService(_path);
        service.Save(new AppSettings());

        Assert.False(service.IsFirstRun);
    }

    [Fact]
    public void File_khong_ton_tai_tra_ve_mac_dinh()
    {
        var service = new SettingsService(_path);
        var settings = service.Load();

        Assert.Equal(HotkeyCombo.Default, settings.Hotkey);
    }

    [Fact]
    public void Ghi_roi_doc_lai_ra_dung()
    {
        var service = new SettingsService(_path);
        var original = new AppSettings
        {
            Hotkey = new HotkeyCombo(HotkeyModifiers.Control | HotkeyModifiers.Shift, "F9"),
            OcrLanguage = "en-US",
            DefaultSaveFolder = @"C:\Temp\Shots"
        };

        service.Save(original);
        var loaded = service.Load();

        Assert.Equal(original.Hotkey, loaded.Hotkey);
        Assert.Equal("en-US", loaded.OcrLanguage);
        Assert.Equal(@"C:\Temp\Shots", loaded.DefaultSaveFolder);
    }

    [Fact]
    public void File_hong_tra_ve_mac_dinh_khong_nem()
    {
        File.WriteAllText(_path, "{ đây không phải JSON hợp lệ ]]}");
        var service = new SettingsService(_path);

        var settings = service.Load(); // không được ném
        Assert.Equal(HotkeyCombo.Default, settings.Hotkey);
    }

    [Fact]
    public void File_rong_tra_ve_mac_dinh()
    {
        File.WriteAllText(_path, "");
        var service = new SettingsService(_path);

        Assert.Equal(HotkeyCombo.Default, service.Load().Hotkey);
    }

    [Fact]
    public void Json_thieu_truong_dung_mac_dinh_cho_truong_do()
    {
        File.WriteAllText(_path, """{ "OcrLanguage": "en-US" }""");
        var service = new SettingsService(_path);

        var settings = service.Load();
        Assert.Equal("en-US", settings.OcrLanguage);
        Assert.Equal(HotkeyCombo.Default, settings.Hotkey);
    }

    [Fact]
    public void Save_tu_tao_thu_muc_neu_chua_co()
    {
        var nested = Path.Combine(_dir, "a", "b", "config.json");
        var service = new SettingsService(nested);

        service.Save(new AppSettings());

        Assert.True(File.Exists(nested));
    }

    [Fact]
    public void Hotkey_khong_hop_le_trong_file_bi_thay_bang_mac_dinh()
    {
        // Người dùng sửa tay file config thành phím trần → phải tự sửa lại
        File.WriteAllText(_path, """{ "Hotkey": { "Modifiers": 0, "Key": "A" } }""");
        var service = new SettingsService(_path);

        Assert.Equal(HotkeyCombo.Default, service.Load().Hotkey);
    }
}
