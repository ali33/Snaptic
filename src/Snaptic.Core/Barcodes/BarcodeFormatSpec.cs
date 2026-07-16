namespace Snaptic.Core.Barcodes;

/// <summary>
/// Luật nội dung hợp lệ của từng định dạng. Hàm thuần, không cần màn hình.
///
/// QR nhận mọi text, nhưng barcode 1D thì KHÔNG: mỗi định dạng có luật riêng.
/// Gõ "hello" vào EAN-13 là lỗi, không phải ra mã.
/// </summary>
public static class BarcodeFormatSpec
{
    private const string Code39Alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-. $/+%";

    public static string DisplayName(SnapticFormat format) => format switch
    {
        SnapticFormat.Qr => "QR",
        SnapticFormat.Code128 => "Code128",
        SnapticFormat.Ean13 => "EAN-13",
        SnapticFormat.UpcA => "UPC-A",
        SnapticFormat.Code39 => "Code39",
        _ => format.ToString()
    };

    public static ValidationResult Validate(SnapticFormat format, string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return ValidationResult.Fail("Nhập nội dung để tạo mã");

        return format switch
        {
            SnapticFormat.Qr => ValidationResult.Ok,
            SnapticFormat.Code128 => ValidateCode128(text),
            SnapticFormat.Ean13 => ValidateDigits(text, 13, 12, "EAN-13"),
            SnapticFormat.UpcA => ValidateDigits(text, 12, 11, "UPC-A"),
            SnapticFormat.Code39 => ValidateCode39(text),
            _ => ValidationResult.Fail($"Định dạng không hỗ trợ: {format}")
        };
    }

    private static ValidationResult ValidateCode128(string text)
        => text.All(c => c <= 127)
            ? ValidationResult.Ok
            : ValidationResult.Fail("Code128 không nhận ký tự có dấu — chỉ ASCII");

    /// <summary>
    /// EAN-13 và UPC-A chỉ nhận đúng số chữ số DỮ LIỆU; số kiểm tra cuối do app tự tính.
    /// Cố tình KHÔNG nhận cả bản đã có số kiểm tra: cho phép cả hai thì sinh mơ hồ
    /// (số cuối là dữ liệu hay là số kiểm tra cần xác thực?).
    /// </summary>
    private static ValidationResult ValidateDigits(
        string text, int totalDigits, int dataDigits, string name)
    {
        if (!text.All(char.IsAsciiDigit))
            return ValidationResult.Fail($"{name} chỉ nhận chữ số");

        if (text.Length != dataDigits)
            return ValidationResult.Fail(
                $"{name} cần đúng {dataDigits} chữ số, đang có {text.Length} " +
                $"(số kiểm tra thứ {totalDigits} app tự tính)");

        return ValidationResult.Ok;
    }

    private static ValidationResult ValidateCode39(string text)
    {
        if (text.Any(char.IsAsciiLetterLower))
            return ValidationResult.Fail("Code39 không nhận chữ thường — dùng CHỮ HOA");

        // Vòng lặp thường, KHÔNG dùng FirstOrDefault. Mẫu `FirstOrDefault(...) != default`
        // có bẫy va chạm sentinel: '\0' chính là default(char), nên ký tự NUL tìm thấy thật
        // lại bị hiểu thành "không tìm thấy" và lọt qua như hợp lệ.
        foreach (var c in text)
        {
            if (Code39Alphabet.Contains(c))
                continue;

            return ValidationResult.Fail(
                $"Code39 không nhận ký tự {Describe(c)} — chỉ A-Z, 0-9, khoảng trắng và - . $ / + %");
        }

        return ValidationResult.Ok;
    }

    /// <summary>
    /// Mô tả ký tự cho lời nhắc. Ký tự điều khiển không in ra được nên hiện mã hex —
    /// báo "không nhận ký tự ''" thì người dùng không hiểu gì.
    /// </summary>
    private static string Describe(char c)
        => char.IsControl(c) ? $"mã điều khiển U+{(int)c:X4}" : $"'{c}'";
}
