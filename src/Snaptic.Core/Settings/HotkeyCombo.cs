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

    public bool IsValid => Modifiers != HotkeyModifiers.None && !string.IsNullOrWhiteSpace(Key);

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
