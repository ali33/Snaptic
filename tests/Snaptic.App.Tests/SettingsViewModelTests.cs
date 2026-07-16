using SkiaSharp;
using Snaptic.App.ViewModels;
using Snaptic.Core.Abstractions;
using Snaptic.Core.Settings;

namespace Snaptic.App.Tests;

public class SettingsViewModelTests : IDisposable
{
    private readonly string _dir;
    private readonly SettingsService _service;

    public SettingsViewModelTests()
    {
        _dir = Path.Combine(Path.GetTempPath(), "snaptic-vm-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_dir);
        _service = new SettingsService(Path.Combine(_dir, "config.json"));
    }

    public void Dispose()
    {
        if (Directory.Exists(_dir)) Directory.Delete(_dir, recursive: true);
    }

    private sealed class FakeOcr : ITextRecognizer
    {
        public IReadOnlyList<string> Languages { get; init; } = ["en-US"];
        public bool IsAvailable => Languages.Count > 0;
        public IReadOnlyList<string> GetAvailableLanguages() => Languages;
        public Task<string?> RecognizeAsync(SKBitmap i, string l, CancellationToken ct = default)
            => Task.FromResult<string?>(null);
    }

    private sealed class FakeHotkey : IHotkeyService
    {
        public bool NextRegisterSucceeds { get; set; } = true;
        public HotkeyCombo? Registered { get; private set; }
        public event EventHandler? Pressed;
        public bool TryRegister(HotkeyCombo combo)
        {
            // PHẢI khớp hợp đồng của WindowsHotkey: đăng ký hỏng thì KHÔNG đụng tới
            // đăng ký đang có. Bản fake cũ giữ nguyên Registered trong mọi trường hợp,
            // trong khi bản thật lúc đó lại XOÁ nó — fake mô hình một hợp đồng mà bản
            // thật không tuân, nên bug "gán hụt là mất phím tắt" vô hình với bộ test.
            if (!NextRegisterSucceeds) return false;
            Registered = combo;
            return true;
        }
        public void Unregister() => Registered = null;
        public void Dispose() { }
        public void FirePressed() => Pressed?.Invoke(this, EventArgs.Empty);
    }

    private sealed class FakeStartup : IStartupService
    {
        private bool _enabled;
        public bool FailToChange { get; set; }
        public int SetCount { get; private set; }
        public FakeStartup(bool initiallyEnabled = false) => _enabled = initiallyEnabled;
        public bool IsEnabled => _enabled;
        public bool TrySetEnabled(bool enabled)
        {
            SetCount++;
            if (FailToChange) return false;
            _enabled = enabled;
            return true;
        }
    }

    private SettingsViewModel Build(
        FakeOcr? ocr = null, FakeHotkey? hotkey = null, FakeStartup? startup = null)
        => new(_service, ocr ?? new FakeOcr(), hotkey ?? new FakeHotkey(), startup ?? new FakeStartup());

    // ---- Ngôn ngữ OCR ----

    [Fact]
    public void Liet_ke_ngon_ngu_tu_OS()
    {
        var vm = Build(new FakeOcr { Languages = ["en-US", "ja-JP"] });
        Assert.Equal(["en-US", "ja-JP"], vm.AvailableLanguages);
        Assert.False(vm.HasNoOcrLanguage);
    }

    [Fact]
    public void Bao_khi_may_chua_cai_goi_OCR_nao()
    {
        var vm = Build(new FakeOcr { Languages = [] });
        Assert.True(vm.HasNoOcrLanguage);
    }

    [Fact]
    public void Chon_san_ngon_ngu_mac_dinh_khi_config_chua_co()
    {
        // Không được để trống: người dùng mở Cài đặt phải thấy ngôn ngữ đang thật sự dùng.
        var vm = Build(new FakeOcr { Languages = ["en-US", "ja-JP"] });
        Assert.Equal("en-US", vm.SelectedLanguage);
    }

    // ---- Đổi phím tắt ----

    [Fact]
    public void Tu_choi_to_hop_khong_co_phim_bo_tro()
    {
        var vm = Build();
        var before = vm.Hotkey;

        Assert.False(vm.TryChangeHotkey(new HotkeyCombo(HotkeyModifiers.None, "A")));
        Assert.Equal(before, vm.Hotkey);          // giữ nguyên phím cũ
        Assert.NotNull(vm.HotkeyError);
    }

    [Fact]
    public void Doi_phim_thanh_cong_thi_cap_nhat()
    {
        var vm = Build();
        var newCombo = new HotkeyCombo(HotkeyModifiers.Control | HotkeyModifiers.Shift, "F9");

        Assert.True(vm.TryChangeHotkey(newCombo));
        Assert.Equal(newCombo, vm.Hotkey);
        Assert.Null(vm.HotkeyError);
    }

    [Fact]
    public void To_hop_bi_chiem_thi_giu_phim_cu_va_bao_loi()
    {
        var hotkey = new FakeHotkey();
        var vm = Build(hotkey: hotkey);
        var before = vm.Hotkey;

        // Phím cũ phải đang ĐƯỢC ĐĂNG KÝ THẬT trước khi thử — nếu không thì assert
        // dưới đây không chứng minh được gì.
        vm.RestoreHotkey();
        Assert.Equal(before, hotkey.Registered);

        hotkey.NextRegisterSucceeds = false;

        Assert.False(vm.TryChangeHotkey(new HotkeyCombo(HotkeyModifiers.Control, "C")));
        Assert.Equal(before, vm.Hotkey);
        Assert.Contains("ứng dụng khác", vm.HotkeyError);

        // ĐÂY mới là câu hỏi mà tên test hứa. Assert vào vm.Hotkey ở trên là đúng theo
        // cấu trúc (TryChangeHotkey chỉ gán khi thành công) nên không bao giờ đỏ được —
        // nó không hỏi gì cả. Câu hỏi thật: phím tắt CÒN SỐNG ở tầng dưới không?
        Assert.Equal(before, hotkey.Registered);
    }

    [Fact]
    public void Phim_khong_ho_tro_bi_tu_choi_va_KHONG_dung_toi_dang_ky_hien_co()
    {
        // Avalonia gọi phím số là "D1". Trước đây nó lọt qua IsValid rồi chết ở tầng
        // Windows, và app đổ lỗi cho "ứng dụng khác" — sai sự thật.
        var hotkey = new FakeHotkey();
        var vm = Build(hotkey: hotkey);
        vm.RestoreHotkey();
        var before = hotkey.Registered;

        Assert.False(vm.TryChangeHotkey(new HotkeyCombo(HotkeyModifiers.Control, "Escape")));

        Assert.Equal(before, hotkey.Registered);
        Assert.NotNull(vm.HotkeyError);
        Assert.DoesNotContain("ứng dụng khác", vm.HotkeyError);   // phải nói ĐÚNG lý do
    }

    [Fact]
    public void To_hop_bi_chiem_thi_dang_ky_lai_duoc_phim_cu()
    {
        var hotkey = new FakeHotkey();
        var vm = Build(hotkey: hotkey);
        var before = vm.Hotkey;

        hotkey.NextRegisterSucceeds = false;
        vm.TryChangeHotkey(new HotkeyCombo(HotkeyModifiers.Control, "C"));

        hotkey.NextRegisterSucceeds = true;
        vm.RestoreHotkey();

        Assert.Equal(before, hotkey.Registered);
    }

    // ---- Khởi động cùng Windows ----

    [Fact]
    public void Doc_trang_thai_khoi_dong_tu_HE_DIEU_HANH_khong_phai_config()
    {
        // Registry là nguồn sự thật. Người dùng tắt trong Task Manager thì Cài đặt
        // phải thấy ngay, không cần đồng bộ gì.
        Assert.True(Build(startup: new FakeStartup(initiallyEnabled: true)).StartWithWindows);
        Assert.False(Build(startup: new FakeStartup(initiallyEnabled: false)).StartWithWindows);
    }

    [Fact]
    public void Doi_o_tick_KHONG_ghi_ngay_ma_cho_toi_luc_Save()
    {
        var startup = new FakeStartup(initiallyEnabled: false);
        var vm = Build(startup: startup);

        vm.StartWithWindows = true;

        Assert.Equal(0, startup.SetCount);   // chưa đụng hệ điều hành
        Assert.False(startup.IsEnabled);
    }

    [Fact]
    public void Save_moi_ap_thay_doi_khoi_dong_cung_Windows()
    {
        var startup = new FakeStartup(initiallyEnabled: false);
        var vm = Build(startup: startup);

        vm.StartWithWindows = true;
        vm.Save();

        Assert.True(startup.IsEnabled);
    }

    [Fact]
    public void Save_KHONG_dung_toi_he_dieu_hanh_khi_o_tick_khong_doi()
    {
        // Ghi registry mỗi lần bấm Lưu dù không đổi gì là đụng vào máy người dùng
        // không lý do.
        var startup = new FakeStartup(initiallyEnabled: true);
        var vm = Build(startup: startup);

        vm.Save();

        Assert.Equal(0, startup.SetCount);
    }

    [Fact]
    public void Bao_loi_khi_khong_bat_duoc_khoi_dong_cung_Windows()
    {
        var startup = new FakeStartup(initiallyEnabled: false) { FailToChange = true };
        var vm = Build(startup: startup);

        vm.StartWithWindows = true;
        vm.Save();

        Assert.NotNull(vm.StartupError);
        Assert.False(vm.StartWithWindows);   // trả ô tick về đúng thực tế
    }

    // ---- Lưu ----

    [Fact]
    public void Save_ghi_xuong_file_va_doc_lai_duoc()
    {
        var vm = Build(new FakeOcr { Languages = ["en-US"] });
        vm.SelectedLanguage = "en-US";
        vm.SaveFolder = @"C:\Temp\X";
        vm.TryChangeHotkey(new HotkeyCombo(HotkeyModifiers.Alt, "P"));

        vm.Save();

        var reloaded = _service.Load();
        Assert.Equal(new HotkeyCombo(HotkeyModifiers.Alt, "P"), reloaded.Hotkey);
        Assert.Equal("en-US", reloaded.OcrLanguage);
        Assert.Equal(@"C:\Temp\X", reloaded.DefaultSaveFolder);
    }

    [Fact]
    public void Nap_lai_gia_tri_da_luu_khi_mo_lai()
    {
        _service.Save(new AppSettings
        {
            Hotkey = new HotkeyCombo(HotkeyModifiers.Win, "S"),
            OcrLanguage = "en-US"
        });

        var vm = Build();

        Assert.Equal(new HotkeyCombo(HotkeyModifiers.Win, "S"), vm.Hotkey);
        Assert.Equal("en-US", vm.SelectedLanguage);
    }
}
