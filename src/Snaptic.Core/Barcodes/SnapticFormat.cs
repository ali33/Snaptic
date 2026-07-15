namespace Snaptic.Core.Barcodes;

/// <summary>Các định dạng mã Snaptic tạo được. Đọc thì ZXing đọc rộng hơn danh sách này.</summary>
public enum SnapticFormat
{
    Qr,
    Code128,
    Ean13,
    UpcA,
    Code39
}
