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

    private Registration? _current;
    private volatile bool _disposed;

    public event EventHandler? Pressed;

    /// <summary>
    /// Đăng ký phím MỚI trước, chỉ khi thành công mới gỡ phím cũ.
    ///
    /// Thứ tự này là điều làm hợp đồng "false = không đổi gì" ĐÚNG DO CẤU TRÚC. Bản đầu
    /// gỡ phím cũ trước rồi mới thử phím mới: thất bại là người dùng mất luôn phím đang
    /// chạy, trong khi thông báo vẫn nói "giữ nguyên phím cũ" — một lời nói dối. Chữa
    /// bằng cách bắt nơi gọi nhớ gọi RestoreHotkey() thì mong manh: chỉ cần một đường
    /// thoát quên gọi là phím tắt chết im lặng.
    ///
    /// Đăng ký hai tổ hợp cùng lúc trong khoảnh khắc chuyển giao là hợp lệ: mỗi luồng có
    /// không gian ID riêng, nên phím cũ và phím mới không đụng nhau.
    /// </summary>
    public bool TryRegister(HotkeyCombo combo)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (!combo.IsValid)
            return false;

        if (ToVirtualKey(combo.Key) is not { } vk)
            return false;

        lock (_gate)
        {
            var candidate = Registration.Start(combo, vk, RaisePressed);
            if (candidate is null)
                return false;      // phím cũ còn nguyên, chưa hề bị đụng tới

            _current?.Stop();
            _current = candidate;
            return true;
        }
    }

    public void Unregister()
    {
        lock (_gate)
        {
            _current?.Stop();
            _current = null;
        }
    }

    private void RaisePressed() => Pressed?.Invoke(this, EventArgs.Empty);

    /// <summary>Một lượt đăng ký đang sống, kèm luồng và vòng lặp thông điệp của nó.</summary>
    private sealed class Registration
    {
        private readonly Thread _thread;
        private uint _threadId;

        private Registration(Thread thread) => _thread = thread;

        /// <summary>Trả null nếu không đăng ký được. Luồng đã được dọn khi đó.</summary>
        public static Registration? Start(HotkeyCombo combo, uint vk, Action onPressed)
        {
            var ready = new ManualResetEventSlim(false);
            var ok = false;
            uint threadId = 0;

            var thread = new Thread(() =>
            {
                threadId = NativeMethods.GetCurrentThreadId();

                var mods = ToWin32Modifiers(combo.Modifiers) | NativeMethods.MOD_NOREPEAT;
                ok = NativeMethods.RegisterHotKey(IntPtr.Zero, HotkeyId, mods, vk);
                ready.Set();

                if (!ok)
                    return;

                try
                {
                    while (NativeMethods.GetMessage(out var msg, IntPtr.Zero, 0, 0) > 0)
                    {
                        if (msg.message == NativeMethods.WM_HOTKEY && msg.wParam.ToInt32() == HotkeyId)
                            onPressed();
                    }
                }
                finally
                {
                    NativeMethods.UnregisterHotKey(IntPtr.Zero, HotkeyId);
                }
            })
            {
                IsBackground = true,
                Name = $"Snaptic hotkey {combo}"
            };

            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();

            // Luồng đặt cờ ngay sau RegisterHotKey, nên đây là chờ kết quả đăng ký chứ
            // không phải chờ cả vòng lặp.
            var signalled = ready.Wait(TimeSpan.FromSeconds(2));
            ready.Dispose();

            if (!signalled || !ok)
            {
                // Hết giờ hoặc đăng ký hỏng. Nếu luồng vẫn sống thì bảo nó thoát; luồng
                // là background nên kể cả không dọn được cũng không giữ tiến trình lại.
                if (threadId != 0)
                    NativeMethods.PostThreadMessage(threadId, NativeMethods.WM_QUIT, IntPtr.Zero, IntPtr.Zero);
                return null;
            }

            return new Registration(thread) { _threadId = threadId };
        }

        public void Stop()
        {
            if (_threadId != 0)
                NativeMethods.PostThreadMessage(_threadId, NativeMethods.WM_QUIT, IntPtr.Zero, IntPtr.Zero);

            _thread.Join(TimeSpan.FromSeconds(2));
            _threadId = 0;
        }
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

    /// <summary>
    /// Tên phím CHUẨN (do <see cref="HotkeyKey"/> ở Core định nghĩa) → mã phím ảo Windows.
    ///
    /// Chỉ nhận tên đã chuẩn hoá. Việc hiểu "D1" của Avalonia nghĩa là "1" thuộc về Core,
    /// nơi có test — đó là chỗ bug từng sống khi bảng này tự nhận cả tên thô.
    /// Trả null nếu không có mã tương ứng.
    /// </summary>
    private static uint? ToVirtualKey(string key)
    {
        if (HotkeyKey.Normalize(key) is not { } name)
            return null;

        if (name.Length == 1)
        {
            var c = name[0];
            if (c is >= 'A' and <= 'Z' or >= '0' and <= '9')
                return c;   // VK của A-Z và 0-9 trùng mã ASCII
        }

        if (name[0] == 'F' && int.TryParse(name[1..], out var fn))
            return (uint)(0x70 + fn - 1);          // VK_F1 = 0x70

        if (name.StartsWith("NumPad", StringComparison.Ordinal))
            return (uint)(0x60 + (name[6] - '0')); // VK_NUMPAD0 = 0x60

        return name switch
        {
            "Space" => 0x20,
            "PageUp" => 0x21,
            "PageDown" => 0x22,
            "End" => 0x23,
            "Home" => 0x24,
            "Left" => 0x25,
            "Up" => 0x26,
            "Right" => 0x27,
            "Down" => 0x28,
            "PrintScreen" => 0x2C,
            "Insert" => 0x2D,
            "Delete" => 0x2E,
            _ => null
        };
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        Unregister();
    }
}
