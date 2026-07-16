namespace Snaptic.Core.Settings;

[Flags]
public enum HotkeyModifiers
{
    None = 0,
    Alt = 1,
    Control = 2,
    Shift = 4,
    Win = 8
}

/// <summary>
/// Tổ hợp phím tắt toàn cục.
/// BẮT BUỘC có ít nhất một phím bổ trợ: phím tắt toàn cục chặn phím trước khi app
/// nhận được, nên gán phím trần sẽ cướp phím đó của toàn hệ thống.
/// </summary>
public readonly record struct HotkeyCombo(HotkeyModifiers Modifiers, string Key)
{
    public static HotkeyCombo Default => new(HotkeyModifiers.Control | HotkeyModifiers.Alt, "Q");

    /// <summary>
    /// Hợp lệ = có phím bổ trợ VÀ <see cref="Key"/> là tên phím CHUẨN.
    ///
    /// Kiểm cả TÊN PHÍM, không chỉ "khác rỗng". Trước đây chỉ kiểm rỗng, nên "D1" của
    /// Avalonia lọt qua đây rồi chết ở tầng Windows — và app báo nhầm thành "tổ hợp bị
    /// ứng dụng khác dùng".
    ///
    /// Đòi tên ĐÃ CHUẨN chứ không phải "chuẩn hoá được": nhận "D1" thì config sẽ lưu
    /// "D1" và ToString() hiện "Ctrl+Alt+D1" — lộ tên enum thô cho người dùng. UI phải
    /// gọi HotkeyKey.Normalize trước khi dựng tổ hợp.
    /// </summary>
    public bool IsValid => Modifiers != HotkeyModifiers.None && HotkeyKey.IsCanonical(Key);

    public override string ToString()
    {
        var parts = new List<string>(5);
        if (Modifiers.HasFlag(HotkeyModifiers.Control)) parts.Add("Ctrl");
        if (Modifiers.HasFlag(HotkeyModifiers.Alt)) parts.Add("Alt");
        if (Modifiers.HasFlag(HotkeyModifiers.Shift)) parts.Add("Shift");
        if (Modifiers.HasFlag(HotkeyModifiers.Win)) parts.Add("Win");
        parts.Add(Key);
        return string.Join("+", parts);
    }
}
