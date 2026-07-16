using System.Runtime.InteropServices;
using SkiaSharp;
using Snaptic.Core.Abstractions;
using Snaptic.Windows.Interop;

namespace Snaptic.Windows;

/// <summary>
/// Clipboard qua Win32 trực tiếp. Avalonia không đặt ảnh vào clipboard tiện như text,
/// và phần này dù chọn stack nào cũng phải viết riêng cho từng OS.
///
/// Clipboard là tài nguyên DÙNG CHUNG toàn hệ thống — app khác có thể đang giữ nó.
/// OpenClipboard hỏng là chuyện thường gặp, không phải phòng xa. Nên có vòng thử lại.
/// </summary>
public sealed class WindowsClipboard : IClipboardService
{
    private const int RetryCount = 10;
    private const int RetryDelayMs = 50;

    public void SetText(string text)
    {
        var bytes = (text.Length + 1) * 2;   // UTF-16 + ký tự kết thúc
        var hMem = NativeMethods.GlobalAlloc(NativeMethods.GMEM_MOVEABLE, (UIntPtr)bytes);
        if (hMem == IntPtr.Zero)
            throw new InvalidOperationException("Không cấp phát được bộ nhớ cho clipboard");

        var ptr = NativeMethods.GlobalLock(hMem);
        if (ptr == IntPtr.Zero)
        {
            NativeMethods.GlobalFree(hMem);
            throw new InvalidOperationException("Không khoá được bộ nhớ clipboard");
        }

        try
        {
            Marshal.Copy(text.ToCharArray(), 0, ptr, text.Length);
            Marshal.WriteInt16(ptr, text.Length * 2, 0);
        }
        finally
        {
            NativeMethods.GlobalUnlock(hMem);
        }

        PutOnClipboard(NativeMethods.CF_UNICODETEXT, hMem);
    }

    public void SetImage(SKBitmap image)
    {
        var dib = ToPackedDib(image);
        var hMem = NativeMethods.GlobalAlloc(NativeMethods.GMEM_MOVEABLE, (UIntPtr)dib.Length);
        if (hMem == IntPtr.Zero)
            throw new InvalidOperationException("Không cấp phát được bộ nhớ cho clipboard");

        var ptr = NativeMethods.GlobalLock(hMem);
        if (ptr == IntPtr.Zero)
        {
            NativeMethods.GlobalFree(hMem);
            throw new InvalidOperationException("Không khoá được bộ nhớ clipboard");
        }

        try
        {
            Marshal.Copy(dib, 0, ptr, dib.Length);
        }
        finally
        {
            NativeMethods.GlobalUnlock(hMem);
        }

        PutOnClipboard(NativeMethods.CF_DIB, hMem);
    }

    /// <summary>
    /// CF_DIB = BITMAPINFOHEADER + pixel, KHÔNG có BITMAPFILEHEADER.
    /// Ghi bottom-up (biHeight dương) vì đó là thứ hầu hết app nhận paste mong đợi.
    /// </summary>
    private static byte[] ToPackedDib(SKBitmap source)
    {
        using var bgra = new SKBitmap(
            new SKImageInfo(source.Width, source.Height, SKColorType.Bgra8888, SKAlphaType.Unpremul));
        if (!source.CopyTo(bgra, SKColorType.Bgra8888))
            throw new InvalidOperationException("Không chuyển được ảnh sang BGRA");

        var width = bgra.Width;
        var height = bgra.Height;
        var stride = width * 4;
        var headerSize = Marshal.SizeOf<NativeMethods.BITMAPINFOHEADER>();
        var buffer = new byte[headerSize + stride * height];

        var header = new NativeMethods.BITMAPINFOHEADER
        {
            biSize = (uint)headerSize,
            biWidth = width,
            biHeight = height,        // dương = bottom-up
            biPlanes = 1,
            biBitCount = 32,
            biCompression = 0,        // BI_RGB
            biSizeImage = (uint)(stride * height)
        };

        var handle = GCHandle.Alloc(buffer, GCHandleType.Pinned);
        try
        {
            Marshal.StructureToPtr(header, handle.AddrOfPinnedObject(), false);
        }
        finally
        {
            handle.Free();
        }

        // Lật dọc: Skia là top-down, DIB bottom-up muốn hàng cuối trước.
        var pixels = bgra.GetPixelSpan();
        for (var row = 0; row < height; row++)
        {
            var src = (height - 1 - row) * stride;
            var dst = headerSize + row * stride;
            pixels.Slice(src, stride).CopyTo(buffer.AsSpan(dst, stride));
        }

        return buffer;
    }

    private static void PutOnClipboard(uint format, IntPtr hMem)
    {
        if (!TryOpenClipboardWithRetry())
        {
            NativeMethods.GlobalFree(hMem);
            throw new InvalidOperationException(
                "Không mở được clipboard — có ứng dụng khác đang giữ. Thử lại sau giây lát.");
        }

        try
        {
            NativeMethods.EmptyClipboard();

            // Sau SetClipboardData thành công, HỆ ĐIỀU HÀNH sở hữu hMem — không được free.
            if (NativeMethods.SetClipboardData(format, hMem) == IntPtr.Zero)
            {
                NativeMethods.GlobalFree(hMem);
                throw new InvalidOperationException("SetClipboardData thất bại");
            }
        }
        finally
        {
            NativeMethods.CloseClipboard();
        }
    }

    private static bool TryOpenClipboardWithRetry()
    {
        for (var i = 0; i < RetryCount; i++)
        {
            if (NativeMethods.OpenClipboard(IntPtr.Zero))
                return true;
            Thread.Sleep(RetryDelayMs);
        }
        return false;
    }
}
