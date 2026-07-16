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

    /// <summary>
    /// Copy SỐ TRẦN, không phải bản hiển thị. Dán "7030 4567 89" có khoảng trắng vào
    /// app ngân hàng là nó báo sai số tài khoản — người dùng phải tự xoá tay.
    /// </summary>
    public void CopyAccountNumber() => _clipboard.SetText(AccountNumber);
}
