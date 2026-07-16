using System.Reflection;
using Snaptic.Core.Abstractions;

namespace Snaptic.App.ViewModels;

/// <summary>Nội dung màn hình Giới thiệu: lời đề tặng và thông tin tài trợ.</summary>
public sealed class AboutViewModel
{
    private readonly IClipboardService _clipboard;

    public AboutViewModel(IClipboardService clipboard) => _clipboard = clipboard;

    public string Dedication =>
        "Snaptic là phần mềm được phát triển và trân trọng dành tặng đồng chí " +
        "Vũ Diệu Huyền — UBND xã Đan Phượng.";

    public string Purpose =>
        "Phần mềm hỗ trợ chụp nhanh vùng màn hình, nhận diện nội dung và tạo mã QR, " +
        "góp phần nâng cao sự thuận tiện và hiệu quả trong công việc hằng ngày.";

    public string DonateCall =>
        "Nếu Snaptic giúp ích cho công việc của bạn, mời tác giả một ly cà phê:";

    public string BankName => "Techcombank";

    public string AccountHolder => "NGUYEN DUC SON";

    /// <summary>Số trần để copy — dán thẳng vào app ngân hàng được.</summary>
    public string AccountNumber => "7030456789";

    /// <summary>Số đã nhóm để đọc bằng mắt. KHÔNG dùng để copy.</summary>
    public string AccountNumberDisplay => "7030 4567 89";

    public string Version =>
        Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "1.0.0";

    public string Copyright => "Copyright © 2026 Nguyen Duc Son";

    /// <summary>
    /// GPL mục 15–16 yêu cầu chương trình TỰ THÔNG BÁO giấy phép và miễn trừ trách nhiệm
    /// cho người dùng — để trong file LICENSE thôi là chưa đủ.
    ///
    /// GPL-3.0 chứ không phải 2.0 là do thư viện quyết định, không phải sở thích:
    /// ZXing.Net là Apache-2.0, mà Apache-2.0 KHÔNG tương thích ngược với GPLv2.
    /// Phát hành dưới GPLv2 sẽ là vi phạm giấy phép của ZXing.Net.
    /// </summary>
    public string License =>
        "Phần mềm tự do theo Giấy phép Công cộng GNU (GNU GPL) phiên bản 3 hoặc mới hơn.";

    public string LicenseUrl => "https://www.gnu.org/licenses/gpl-3.0.html";

    /// <summary>
    /// Tóm tắt tiếng Việt của GPL mục 15–16. Văn bản có hiệu lực pháp lý vẫn là bản
    /// tiếng Anh trong LICENSE — nói rõ điều đó để không ai hiểu nhầm bản tóm tắt này
    /// là văn bản pháp lý.
    /// </summary>
    public string Disclaimer =>
        "Phần mềm được cung cấp NGUYÊN TRẠNG, KHÔNG kèm bất kỳ bảo đảm nào. " +
        "Tác giả không chịu trách nhiệm với mọi thiệt hại phát sinh từ việc sử dụng. " +
        "Bạn chịu toàn bộ rủi ro về chất lượng và hiệu năng.";

    /// <summary>Giới hạn cụ thể của Snaptic mà người dùng cần biết trước khi tin kết quả.</summary>
    public string KnownLimits =>
        "Lưu ý: OCR chữ Việt CÓ DẤU cho kết quả sai mà trông hợp lý — luôn kiểm lại bằng mắt. " +
        "Mã QR là dữ liệu từ nguồn không tin cậy; app chỉ mở link http/https nhưng không " +
        "đảm bảo trang đích an toàn.";

    /// <summary>
    /// Copy SỐ TRẦN, không phải bản hiển thị. Dán "7030 4567 89" có khoảng trắng vào
    /// app ngân hàng là nó báo sai số tài khoản — người dùng phải tự xoá tay.
    /// </summary>
    public void CopyAccountNumber() => _clipboard.SetText(AccountNumber);
}
