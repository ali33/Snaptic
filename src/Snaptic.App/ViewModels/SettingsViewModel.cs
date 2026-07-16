using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using Snaptic.Core.Abstractions;
using Snaptic.Core.Recognition;
using Snaptic.Core.Settings;

namespace Snaptic.App.ViewModels;

/// <summary>
/// Logic màn hình Cài đặt.
///
/// Hai nhóm thiết lập hành xử KHÁC NHAU, và điều đó là cố ý:
///
/// • Phím tắt áp NGAY khi gán — bắt buộc, vì cách duy nhất biết tổ hợp có bị app khác
///   chiếm hay không là thử đăng ký thật. Bấm Huỷ thì <see cref="RestoreHotkey"/> trả lại.
///
/// • Khởi động cùng Windows áp lúc BẤM LƯU — nó ghi vào registry của người dùng, không
///   nên đụng vào chỉ vì họ tick thử rồi đổi ý.
/// </summary>
public sealed class SettingsViewModel : INotifyPropertyChanged
{
    private readonly SettingsService _settingsService;
    private readonly IHotkeyService _hotkeyService;
    private readonly IStartupService _startupService;

    private HotkeyCombo _hotkey;
    private string? _hotkeyError;
    private string? _startupError;
    private bool _startWithWindows;

    public SettingsViewModel(
        SettingsService settingsService,
        ITextRecognizer ocr,
        IHotkeyService hotkeyService,
        IStartupService startupService)
    {
        _settingsService = settingsService;
        _hotkeyService = hotkeyService;
        _startupService = startupService;

        var settings = settingsService.Load();
        _hotkey = settings.Hotkey;

        AvailableLanguages = ocr.GetAvailableLanguages();

        // Nếu người dùng chưa tự chọn thì hiện đúng thứ app ĐANG dùng, không để trống.
        // Cùng một hàm mà đường chụp dùng — hai chỗ không được lệch nhau.
        SelectedLanguage = settings.OcrLanguage
                           ?? OcrLanguagePicker.Pick(AvailableLanguages, CultureInfo.CurrentUICulture.Name);

        SaveFolder = settings.DefaultSaveFolder;

        // Hỏi HỆ ĐIỀU HÀNH chứ không đọc config: người dùng tắt trong Task Manager
        // thì màn Cài đặt phải thấy ngay.
        _startWithWindows = startupService.IsEnabled;
    }

    public IReadOnlyList<string> AvailableLanguages { get; }

    /// <summary>True khi máy chưa cài gói OCR nào — UI hiện hướng dẫn cài.</summary>
    public bool HasNoOcrLanguage => AvailableLanguages.Count == 0;

    public string? SelectedLanguage { get; set; }

    public string SaveFolder { get; set; }

    public HotkeyCombo Hotkey => _hotkey;

    public string? HotkeyError => _hotkeyError;

    public string? StartupError => _startupError;

    public bool StartWithWindows
    {
        get => _startWithWindows;
        set
        {
            if (_startWithWindows == value) return;
            _startWithWindows = value;
            Raise();
        }
    }

    /// <summary>
    /// Thử đăng ký NGAY khi người dùng gán. Hỏng → giữ nguyên phím cũ và báo lỗi.
    /// KHÔNG được im lặng nhận rồi để người dùng phát hiện sau.
    /// </summary>
    public bool TryChangeHotkey(HotkeyCombo combo)
    {
        if (!combo.IsValid)
        {
            SetHotkeyError("Phím tắt phải có ít nhất một phím bổ trợ (Ctrl, Alt, Shift hoặc Win)");
            return false;
        }

        if (!_hotkeyService.TryRegister(combo))
        {
            SetHotkeyError($"Tổ hợp {combo} đang bị ứng dụng khác dùng — giữ nguyên {_hotkey}");
            return false;
        }

        _hotkey = combo;
        SetHotkeyError(null);
        Raise(nameof(Hotkey));
        return true;
    }

    /// <summary>Đăng ký lại phím hiện tại — gọi khi bấm Huỷ, hoặc sau một lần gán hỏng.</summary>
    public void RestoreHotkey() => _hotkeyService.TryRegister(_hotkey);

    public void Save()
    {
        // Chỉ đụng registry khi ô tick THẬT SỰ đổi. Ghi mỗi lần bấm Lưu dù không đổi gì
        // là can thiệp vào máy người dùng không lý do.
        if (_startWithWindows != _startupService.IsEnabled)
        {
            if (_startupService.TrySetEnabled(_startWithWindows))
            {
                SetStartupError(null);
            }
            else
            {
                SetStartupError(_startWithWindows
                    ? "Không bật được khởi động cùng Windows — có thể bị chính sách hệ thống chặn"
                    : "Không tắt được khởi động cùng Windows");

                // Trả ô tick về ĐÚNG THỰC TẾ thay vì để nó nói dối.
                _startWithWindows = _startupService.IsEnabled;
                Raise(nameof(StartWithWindows));
            }
        }

        _settingsService.Save(new AppSettings
        {
            Hotkey = _hotkey,
            OcrLanguage = SelectedLanguage,
            DefaultSaveFolder = SaveFolder
        });
    }

    private void SetHotkeyError(string? message)
    {
        _hotkeyError = message;
        Raise(nameof(HotkeyError));
    }

    private void SetStartupError(string? message)
    {
        _startupError = message;
        Raise(nameof(StartupError));
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void Raise([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
