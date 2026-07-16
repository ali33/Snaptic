using Snaptic.Core.Abstractions;
using Snaptic.Core.Settings;
using Snaptic.Windows.Interop;

namespace Snaptic.Windows;

/// <summary>
/// Phím tắt toàn cục qua RegisterHotKey.
///
/// Chạy trên LUỒNG RIÊNG có vòng lặp thông điệp của chính nó. Lý do: RegisterHotKey gửi
/// WM_HOTKEY vào hàng đợi thông điệp của luồng đã đăng ký, và can thiệp vào vòng lặp
/// thông điệp của Avalonia thì mong manh hơn nhiều so với tự nuôi một luồng nhỏ.
///
/// Sự kiện <see cref="Pressed"/> phát trên luồng nền — người nghe PHẢI tự chuyển về
/// luồng UI trước khi đụng vào giao diện.
/// </summary>
public sealed class WindowsHotkey : IHotkeyService
{
    private const int HotkeyId = 1;

    private readonly Lock _gate = new();
    private readonly ManualResetEventSlim _registerDone = new(false);

    private Thread? _thread;
    private uint _threadId;
    private bool _registerResult;
    private volatile bool _disposed;

    public event EventHandler? Pressed;

    public bool TryRegister(HotkeyCombo combo)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (!combo.IsValid)
            return false;

        if (ToVirtualKey(combo.Key) == 0)
            return false;

        lock (_gate)
        {
            UnregisterCore();

            _registerDone.Reset();
            _thread = new Thread(() => MessageLoop(combo))
            {
                IsBackground = true,
                Name = "Snaptic hotkey"
            };
            _thread.SetApartmentState(ApartmentState.STA);
            _thread.Start();

            // Luồng đặt _registerDone ngay sau khi RegisterHotKey trả về, nên chờ ở đây
            // là chờ kết quả đăng ký chứ không phải chờ cả vòng lặp.
            if (!_registerDone.Wait(TimeSpan.FromSeconds(2)))
            {
                UnregisterCore();
                return false;
            }

            if (!_registerResult)
            {
                UnregisterCore();
                return false;
            }

            return true;
        }
    }

    private void MessageLoop(HotkeyCombo combo)
    {
        _threadId = NativeMethods.GetCurrentThreadId();

        var mods = ToWin32Modifiers(combo.Modifiers) | NativeMethods.MOD_NOREPEAT;
        var vk = ToVirtualKey(combo.Key);

        _registerResult = NativeMethods.RegisterHotKey(IntPtr.Zero, HotkeyId, mods, vk);
        _registerDone.Set();

        if (!_registerResult)
            return;

        try
        {
            while (NativeMethods.GetMessage(out var msg, IntPtr.Zero, 0, 0) > 0)
            {
                if (msg.message == NativeMethods.WM_HOTKEY && msg.wParam.ToInt32() == HotkeyId)
                    Pressed?.Invoke(this, EventArgs.Empty);
            }
        }
        finally
        {
            NativeMethods.UnregisterHotKey(IntPtr.Zero, HotkeyId);
        }
    }

    public void Unregister()
    {
        lock (_gate)
            UnregisterCore();
    }

    /// <summary>Gọi khi đã giữ <see cref="_gate"/>.</summary>
    private void UnregisterCore()
    {
        if (_thread is null)
            return;

        if (_threadId != 0)
            NativeMethods.PostThreadMessage(_threadId, NativeMethods.WM_QUIT, IntPtr.Zero, IntPtr.Zero);

        _thread.Join(TimeSpan.FromSeconds(2));
        _thread = null;
        _threadId = 0;
    }

    private static uint ToWin32Modifiers(HotkeyModifiers mods)
    {
        uint result = 0;
        if (mods.HasFlag(HotkeyModifiers.Alt)) result |= NativeMethods.MOD_ALT;
        if (mods.HasFlag(HotkeyModifiers.Control)) result |= NativeMethods.MOD_CONTROL;
        if (mods.HasFlag(HotkeyModifiers.Shift)) result |= NativeMethods.MOD_SHIFT;
        if (mods.HasFlag(HotkeyModifiers.Win)) result |= NativeMethods.MOD_WIN;
        return result;
    }

    /// <summary>Đổi tên phím sang mã phím ảo. Trả 0 nếu không nhận ra.</summary>
    private static uint ToVirtualKey(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
            return 0;

        key = key.Trim().ToUpperInvariant();

        if (key.Length == 1)
        {
            var c = key[0];
            if (c is >= 'A' and <= 'Z') return c;
            if (c is >= '0' and <= '9') return c;
        }

        if (key.Length > 1 && key[0] == 'F'
            && int.TryParse(key[1..], out var fn) && fn is >= 1 and <= 24)
            return (uint)(0x70 + fn - 1);   // VK_F1 = 0x70

        return key switch
        {
            "SPACE" => 0x20,
            "INSERT" => 0x2D,
            "DELETE" => 0x2E,
            "HOME" => 0x24,
            "END" => 0x23,
            "PRINTSCREEN" => 0x2C,
            _ => 0
        };
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        Unregister();
        _registerDone.Dispose();
    }
}
