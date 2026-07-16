namespace Snaptic.Core.Abstractions;

/// <summary>
/// Bật/tắt việc app tự khởi động cùng hệ điều hành. Hiện thực bởi tầng platform.
///
/// CỐ Ý không lưu trạng thái này vào AppSettings: hệ điều hành mới là nguồn sự thật.
/// Người dùng tắt Snaptic trong Task Manager > Startup thì config sẽ nói "bật" còn
/// thực tế là "tắt" — hai nguồn đá nhau. Luôn hỏi OS.
/// </summary>
public interface IStartupService
{
    /// <summary>False nếu không đọc được trạng thái (coi như chưa bật).</summary>
    bool IsEnabled { get; }

    /// <summary>Trả false nếu không đổi được (vd bị chính sách hệ thống chặn). KHÔNG ném.</summary>
    bool TrySetEnabled(bool enabled);
}
