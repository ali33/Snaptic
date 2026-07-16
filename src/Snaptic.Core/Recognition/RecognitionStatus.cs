namespace Snaptic.Core.Recognition;

/// <summary>
/// Trạng thái một phép nhận diện. Đây là thứ điều khiển việc nút MỌC DẦN trong
/// cửa sổ preview: barcode xong sau ~10ms, OCR xong sau ~300ms.
/// </summary>
public enum RecognitionStatus
{
    /// <summary>Đang chạy — chưa hiện nút.</summary>
    Pending,

    /// <summary>Xong — hiện nút nếu có kết quả.</summary>
    Done,

    /// <summary>Máy không có engine — ẩn nút vĩnh viễn.</summary>
    Unavailable,

    /// <summary>Chạy lỗi — ẩn nút, ghi log.</summary>
    Failed
}
