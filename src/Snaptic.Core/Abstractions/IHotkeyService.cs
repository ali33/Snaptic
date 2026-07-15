using Snaptic.Core.Settings;

namespace Snaptic.Core.Abstractions;

/// <summary>Phím tắt toàn cục. Hiện thực bởi tầng platform.</summary>
public interface IHotkeyService : IDisposable
{
    /// <summary>False khi tổ hợp đã bị app khác chiếm. KHÔNG ném.</summary>
    bool TryRegister(HotkeyCombo combo);

    void Unregister();

    event EventHandler? Pressed;
}
