namespace Snaptic.Core.Links;

/// <summary>
/// Quyết định URL nào được phép mở từ nội dung mã quét được.
/// Mã QR là dữ liệu từ bên ngoài — chỉ http/https được mở.
/// Đây là hàng rào duy nhất giữa nội dung lạ và máy người dùng.
/// </summary>
public static class LinkValidator
{
    public static bool IsOpenableUrl(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return false;

        if (!Uri.TryCreate(text.Trim(), UriKind.Absolute, out var uri))
            return false;

        return uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps;
    }
}
