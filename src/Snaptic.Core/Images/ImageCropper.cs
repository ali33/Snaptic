using SkiaSharp;
using Snaptic.Core.Display;

namespace Snaptic.Core.Images;

/// <summary>
/// Cắt một vùng ra khỏi ảnh đã đóng băng.
///
/// Nằm ở Core (chứ không ở tầng App cùng CaptureCoordinator) vì nó chứa một cái bẫy
/// tinh vi cần test canh giữ — xem <see cref="Crop"/>.
/// </summary>
public static class ImageCropper
{
    /// <summary>
    /// Cắt <paramref name="rect"/> (toạ độ physical TOÀN CỤC) ra khỏi <paramref name="source"/>
    /// (ảnh đóng băng, đánh số từ 0). <paramref name="originX"/>/<paramref name="originY"/> là
    /// gốc của desktop ảo — CÓ THỂ ÂM khi màn phụ nằm bên trái hoặc bên trên màn chính.
    ///
    /// TUYỆT ĐỐI KHÔNG dùng SKBitmap.ExtractSubset ở đây. Nó tạo bitmap DÙNG CHUNG bộ nhớ
    /// với ảnh gốc, nên RowBytes vẫn là stride của ảnh GỐC chứ không phải Width*4. Thư viện
    /// nào đọc buffer như dữ liệu liền mạch — ZXing chẳng hạn — sẽ thấy mỗi hàng lệch đi và
    /// ảnh bị xé chéo, decode ra null.
    ///
    /// Cái bẫy nằm ở chỗ nó KHÔNG LỘ RA: encode PNG có tôn trọng RowBytes nên cửa sổ preview
    /// vẫn hiện ảnh hoàn hảo. Nhìn bằng mắt thì mọi thứ đúng, chỉ ZXing thấy rác.
    /// Đây là bug có thật, sống sót qua 14 task, chỉ lộ khi người dùng chụp một QR thật.
    ///
    /// Vẽ sang bitmap mới bằng Canvas là có bộ nhớ riêng, liền mạch, RowBytes == Width*4.
    /// </summary>
    public static SKBitmap Crop(SKBitmap source, PhysicalRect rect, int originX, int originY)
    {
        // Đổi từ toạ độ desktop ảo sang toạ độ trong ảnh đóng băng.
        var x = rect.X - originX;
        var y = rect.Y - originY;

        // Kẹp vào biên ảnh — vùng chọn có thể tràn ra vùng không thuộc monitor nào
        // (desktop ảo là hình chữ nhật nhưng các màn có thể xếp so le).
        x = Math.Clamp(x, 0, Math.Max(0, source.Width - 1));
        y = Math.Clamp(y, 0, Math.Max(0, source.Height - 1));
        var w = Math.Clamp(rect.Width, 1, source.Width - x);
        var h = Math.Clamp(rect.Height, 1, source.Height - y);

        var result = new SKBitmap(new SKImageInfo(w, h, source.ColorType, source.AlphaType));
        using var canvas = new SKCanvas(result);
        canvas.DrawBitmap(source, new SKRectI(x, y, x + w, y + h), new SKRect(0, 0, w, h));
        return result;
    }
}
