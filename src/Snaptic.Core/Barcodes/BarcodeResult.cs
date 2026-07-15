namespace Snaptic.Core.Barcodes;

/// <summary>
/// Kết quả đọc được một mã từ ảnh.
/// <paramref name="Format"/> là tên định dạng ZXing trả về (rộng hơn <see cref="SnapticFormat"/>
/// vì ZXing đọc được cả những định dạng app không tạo, ví dụ DATA_MATRIX).
/// <paramref name="IsQr"/> quyết định nhãn nút: "Copy QR text" hay "Copy barcode".
/// <paramref name="IsHttpUrl"/> quyết định có hiện nút Open link không.
/// </summary>
public sealed record BarcodeResult(string Format, string Text, bool IsQr, bool IsHttpUrl);
