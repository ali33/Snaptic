namespace Snaptic.Core.Recognition;

/// <summary>
/// Chọn ngôn ngữ OCR mặc định khi người dùng chưa tự chọn.
///
/// NẰM Ở CORE, KHÔNG PHẢI Ở SETTINGS. Bản kế hoạch đầu đặt logic này trong
/// SettingsViewModel — sai chỗ, và hậu quả có thật: đường CHỤP mới là chỗ cần nó, nên
/// người dùng không bao giờ mở Cài đặt thì OcrLanguage giữ nguyên null và OCR IM LẶNG
/// không chạy, không báo gì. Đây là quy tắc của cả ứng dụng chứ không phải của riêng
/// một màn hình.
/// </summary>
public static class OcrLanguagePicker
{
    /// <summary>
    /// Thứ tự ưu tiên: ngôn ngữ hệ thống nếu máy đọc được → tiếng Anh → cái đầu tiên có.
    /// Trả null CHỈ KHI máy không có engine OCR nào.
    ///
    /// Ca thực tế của người dùng Việt: hệ thống vi-VN, mà Windows không phát hành gói
    /// OCR tiếng Việt (spec §8 R1) → tự lùi về en-US.
    /// </summary>
    /// <param name="available">Thẻ BCP-47 máy đọc được, ví dụ "en-US".</param>
    /// <param name="systemTag">Thẻ ngôn ngữ hệ thống. Rỗng cũng chấp nhận được.</param>
    public static string? Pick(IReadOnlyList<string> available, string? systemTag)
    {
        if (available.Count == 0)
            return null;

        if (!string.IsNullOrWhiteSpace(systemTag))
        {
            // Khớp đúng hết thẻ trước (en-US == en-US).
            var exact = available.FirstOrDefault(
                l => string.Equals(l, systemTag, StringComparison.OrdinalIgnoreCase));
            if (exact is not null)
                return exact;

            // Rồi mới khớp theo phần ngôn ngữ: hệ thống en-AU mà máy có en-US thì
            // vẫn nên chọn en-US chứ không nhảy sang ja-JP.
            var prefix = LanguagePart(systemTag);
            var sameLanguage = available.FirstOrDefault(
                l => string.Equals(LanguagePart(l), prefix, StringComparison.OrdinalIgnoreCase));
            if (sameLanguage is not null)
                return sameLanguage;
        }

        var english = available.FirstOrDefault(
            l => string.Equals(LanguagePart(l), "en", StringComparison.OrdinalIgnoreCase));

        return english ?? available[0];
    }

    /// <summary>"en-US" → "en". Thẻ không có dấu gạch thì trả nguyên.</summary>
    private static string LanguagePart(string tag)
    {
        var dash = tag.IndexOf('-');
        return dash < 0 ? tag : tag[..dash];
    }
}
