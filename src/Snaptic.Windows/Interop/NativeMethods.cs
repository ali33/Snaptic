using System.Runtime.InteropServices;

namespace Snaptic.Windows.Interop;

/// <summary>Toàn bộ P/Invoke của tầng Windows gom về đây. Không rải ra nơi khác.</summary>
internal static class NativeMethods
{
    // ---- Chỉ số hệ thống cho desktop ảo ----
    internal const int SM_XVIRTUALSCREEN = 76;
    internal const int SM_YVIRTUALSCREEN = 77;
    internal const int SM_CXVIRTUALSCREEN = 78;
    internal const int SM_CYVIRTUALSCREEN = 79;

    // ---- BitBlt ----
    internal const int SRCCOPY = 0x00CC0020;
    internal const int CAPTUREBLT = 0x40000000;  // cần để bắt cả lớp layered window

    [DllImport("user32.dll")]
    internal static extern int GetSystemMetrics(int nIndex);

    [DllImport("user32.dll")]
    internal static extern IntPtr GetDC(IntPtr hWnd);

    [DllImport("user32.dll")]
    internal static extern int ReleaseDC(IntPtr hWnd, IntPtr hDC);

    [DllImport("gdi32.dll")]
    internal static extern IntPtr CreateCompatibleDC(IntPtr hdc);

    [DllImport("gdi32.dll")]
    internal static extern IntPtr CreateCompatibleBitmap(IntPtr hdc, int width, int height);

    [DllImport("gdi32.dll")]
    internal static extern IntPtr SelectObject(IntPtr hdc, IntPtr hObject);

    [DllImport("gdi32.dll")]
    internal static extern bool DeleteObject(IntPtr hObject);

    [DllImport("gdi32.dll")]
    internal static extern bool DeleteDC(IntPtr hdc);

    [DllImport("gdi32.dll")]
    internal static extern bool BitBlt(
        IntPtr hdcDest, int xDest, int yDest, int w, int h,
        IntPtr hdcSrc, int xSrc, int ySrc, int rop);

    [DllImport("gdi32.dll")]
    internal static extern int GetDIBits(
        IntPtr hdc, IntPtr hbmp, uint start, uint cLines,
        byte[] lpvBits, ref BITMAPINFO lpbmi, uint usage);

    internal const uint DIB_RGB_COLORS = 0;

    [StructLayout(LayoutKind.Sequential)]
    internal struct BITMAPINFOHEADER
    {
        public uint biSize;
        public int biWidth;
        public int biHeight;
        public ushort biPlanes;
        public ushort biBitCount;
        public uint biCompression;
        public uint biSizeImage;
        public int biXPelsPerMeter;
        public int biYPelsPerMeter;
        public uint biClrUsed;
        public uint biClrImportant;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct BITMAPINFO
    {
        public BITMAPINFOHEADER bmiHeader;
        public uint bmiColors;
    }

    // ---- Liệt kê monitor ----
    [StructLayout(LayoutKind.Sequential)]
    internal struct RECT
    {
        public int Left, Top, Right, Bottom;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    internal struct MONITORINFOEX
    {
        public uint cbSize;
        public RECT rcMonitor;
        public RECT rcWork;
        public uint dwFlags;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        public string szDevice;
    }

    internal delegate bool MonitorEnumProc(IntPtr hMonitor, IntPtr hdc, ref RECT rect, IntPtr data);

    [DllImport("user32.dll")]
    internal static extern bool EnumDisplayMonitors(
        IntPtr hdc, IntPtr clip, MonitorEnumProc callback, IntPtr data);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    internal static extern bool GetMonitorInfo(IntPtr hMonitor, ref MONITORINFOEX info);

    internal enum MONITOR_DPI_TYPE { MDT_EFFECTIVE_DPI = 0 }

    [DllImport("Shcore.dll")]
    internal static extern int GetDpiForMonitor(
        IntPtr hmonitor, MONITOR_DPI_TYPE dpiType, out uint dpiX, out uint dpiY);

    // ---- Clipboard ----
    internal const uint CF_DIB = 8;
    internal const uint CF_UNICODETEXT = 13;

    [DllImport("user32.dll", SetLastError = true)]
    internal static extern bool OpenClipboard(IntPtr hWndNewOwner);

    [DllImport("user32.dll", SetLastError = true)]
    internal static extern bool CloseClipboard();

    [DllImport("user32.dll", SetLastError = true)]
    internal static extern bool EmptyClipboard();

    [DllImport("user32.dll", SetLastError = true)]
    internal static extern IntPtr SetClipboardData(uint uFormat, IntPtr hMem);

    [DllImport("user32.dll", SetLastError = true)]
    internal static extern IntPtr GetClipboardData(uint uFormat);

    [DllImport("user32.dll")]
    internal static extern bool IsClipboardFormatAvailable(uint format);

    [DllImport("kernel32.dll")]
    internal static extern IntPtr GlobalAlloc(uint uFlags, UIntPtr dwBytes);

    [DllImport("kernel32.dll")]
    internal static extern IntPtr GlobalLock(IntPtr hMem);

    [DllImport("kernel32.dll")]
    internal static extern bool GlobalUnlock(IntPtr hMem);

    [DllImport("kernel32.dll")]
    internal static extern IntPtr GlobalFree(IntPtr hMem);

    [DllImport("kernel32.dll")]
    internal static extern UIntPtr GlobalSize(IntPtr hMem);

    internal const uint GMEM_MOVEABLE = 0x0002;

    // ---- Phím tắt toàn cục ----
    [DllImport("user32.dll", SetLastError = true)]
    internal static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll", SetLastError = true)]
    internal static extern bool UnregisterHotKey(IntPtr hWnd, int id);

    internal const uint MOD_ALT = 0x0001;
    internal const uint MOD_CONTROL = 0x0002;
    internal const uint MOD_SHIFT = 0x0004;
    internal const uint MOD_WIN = 0x0008;
    internal const uint MOD_NOREPEAT = 0x4000;
    internal const int WM_HOTKEY = 0x0312;
    internal const uint WM_QUIT = 0x0012;

    [StructLayout(LayoutKind.Sequential)]
    internal struct MSG
    {
        public IntPtr hwnd;
        public uint message;
        public IntPtr wParam;
        public IntPtr lParam;
        public uint time;
        public int ptX;
        public int ptY;
    }

    [DllImport("user32.dll")]
    internal static extern int GetMessage(out MSG lpMsg, IntPtr hWnd, uint min, uint max);

    [DllImport("user32.dll", SetLastError = true)]
    internal static extern bool PostThreadMessage(uint idThread, uint msg, IntPtr wParam, IntPtr lParam);

    [DllImport("kernel32.dll")]
    internal static extern uint GetCurrentThreadId();
}
