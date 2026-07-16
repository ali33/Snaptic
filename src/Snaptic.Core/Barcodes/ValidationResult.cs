namespace Snaptic.Core.Barcodes;

/// <summary>
/// Kết quả kiểm tra nội dung có hợp lệ với định dạng không.
/// <see cref="Hint"/> là lời nhắc cụ thể hiện cho người dùng, null khi hợp lệ.
/// </summary>
public readonly record struct ValidationResult(bool IsValid, string? Hint)
{
    public static ValidationResult Ok => new(true, null);
    public static ValidationResult Fail(string hint) => new(false, hint);
}
