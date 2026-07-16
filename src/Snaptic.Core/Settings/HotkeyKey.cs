namespace Snaptic.Core.Settings;

/// <summary>
/// Tên phím hợp lệ cho phím tắt, và cách chuẩn hoá tên do UI đưa lên.
///
/// NẰM Ở CORE vì đây là logic có nhánh và phải test được. Trước đây nó là một switch
/// trong tầng Windows — tầng cố ý không có test — và đó chính là chỗ bug sống: Avalonia
/// đặt tên phím số là "D1" chứ không phải "1", switch kia không hiểu, app báo "tổ hợp bị
/// ứng dụng khác dùng" cho một tổ hợp chẳng ai dùng.
///
/// Ranh giới: chỗ này chỉ biết TÊN phím. Việc đổi tên sang mã phím ảo là chuyện của
/// tầng platform — Windows và macOS dùng bảng mã khác nhau.
/// </summary>
public static class HotkeyKey
{
    /// <summary>
    /// Các phím đặt tên được hỗ trợ. Cố ý KHÔNG có Escape/Tab/Enter/Backspace: Windows
    /// giữ riêng hoặc chúng quá thiết yếu để cướp làm phím tắt toàn cục.
    /// </summary>
    private static readonly string[] NamedKeys =
    [
        "Space", "Insert", "Delete", "Home", "End", "PageUp", "PageDown",
        "Left", "Right", "Up", "Down", "PrintScreen"
    ];

    /// <summary>
    /// Đổi tên phím do UI đưa lên thành tên chuẩn của Snaptic, hoặc null nếu không hỗ trợ.
    /// Idempotent: đưa tên đã chuẩn vào thì trả lại chính nó (config lưu tên đã chuẩn).
    ///
    /// Điểm mấu chốt: Avalonia gọi phím số là "D0".."D9". Không đổi về "0".."9" thì
    /// người dùng không bao giờ gán được Ctrl+Alt+1.
    /// </summary>
    public static string? Normalize(string? keyName)
    {
        if (string.IsNullOrWhiteSpace(keyName))
            return null;

        var key = keyName.Trim();

        // Chữ cái đơn và chữ số đơn (chữ số đến từ config đã chuẩn hoá).
        if (key.Length == 1)
        {
            var c = char.ToUpperInvariant(key[0]);
            if (c is >= 'A' and <= 'Z' or >= '0' and <= '9')
                return c.ToString();
        }

        // "D1" -> "1". Đây là tên Avalonia dùng cho hàng phím số.
        if (key.Length == 2
            && char.ToUpperInvariant(key[0]) == 'D'
            && char.IsAsciiDigit(key[1]))
            return key[1].ToString();

        // "F1".."F24"
        if (key.Length is 2 or 3
            && char.ToUpperInvariant(key[0]) == 'F'
            && int.TryParse(key[1..], out var fn)
            && fn is >= 1 and <= 24)
            return $"F{fn}";

        // "NumPad0".."NumPad9"
        if (key.Length == 7
            && key.StartsWith("NumPad", StringComparison.OrdinalIgnoreCase)
            && char.IsAsciiDigit(key[6]))
            return $"NumPad{key[6]}";

        return NamedKeys.FirstOrDefault(
            n => string.Equals(n, key, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Tên này ĐÃ LÀ tên chuẩn hay chưa. Khác với "chuẩn hoá được": "D1" chuẩn hoá được
    /// thành "1" nhưng bản thân nó KHÔNG chuẩn.
    ///
    /// Phân biệt hai thứ này là cần thiết. Nếu HotkeyCombo chấp nhận "D1" là hợp lệ thì
    /// config sẽ lưu "D1" và ToString() hiện "Ctrl+Alt+D1" — đúng cái tên enum thô mà ta
    /// đang cố giấu khỏi người dùng. UI phải Normalize TRƯỚC khi dựng HotkeyCombo.
    /// </summary>
    public static bool IsCanonical(string? keyName)
        => keyName is not null && Normalize(keyName) == keyName;

    /// <summary>Có đổi được thành một tên chuẩn không (dùng cho tên thô do UI đưa lên).</summary>
    public static bool CanNormalize(string? keyName) => Normalize(keyName) is not null;
}
