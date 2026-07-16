using Microsoft.Win32;
using Snaptic.Core.Abstractions;

namespace Snaptic.Windows;

/// <summary>
/// Tự khởi động cùng Windows qua khoá Run của người dùng hiện tại.
///
/// Dùng HKEY_CURRENT_USER chứ không phải HKEY_LOCAL_MACHINE: HKCU không cần quyền admin,
/// và app này là công cụ cá nhân — không có lý do gì đăng ký cho mọi tài khoản trên máy.
///
/// Khoá này cũng chính là thứ Task Manager > Startup hiển thị, nên người dùng tắt ở đó
/// thì IsEnabled tự thấy ngay — không cần đồng bộ gì.
/// </summary>
public sealed class WindowsStartupService : IStartupService
{
    private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string ValueName = "Snaptic";

    public bool IsEnabled
    {
        get
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: false);
                return key?.GetValue(ValueName) is string s && !string.IsNullOrWhiteSpace(s);
            }
            catch (Exception ex) when (ex is System.Security.SecurityException or UnauthorizedAccessException)
            {
                return false;
            }
        }
    }

    public bool TrySetEnabled(bool enabled)
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: true)
                            ?? Registry.CurrentUser.CreateSubKey(RunKeyPath);

            if (key is null)
                return false;

            if (enabled)
            {
                if (ExecutablePath() is not { } path)
                    return false;

                // Bọc nháy kép: đường dẫn có khoảng trắng mà không bọc thì Windows
                // cắt ở dấu cách đầu tiên và chạy nhầm file.
                key.SetValue(ValueName, $"\"{path}\"", RegistryValueKind.String);
            }
            else
            {
                key.DeleteValue(ValueName, throwOnMissingValue: false);
            }

            return true;
        }
        catch (Exception ex) when (ex is System.Security.SecurityException
                                      or UnauthorizedAccessException
                                      or IOException)
        {
            // Chính sách nhóm có thể khoá khoá Run. Không đổi được thì báo false,
            // không phải lý do làm sập app.
            return false;
        }
    }

    /// <summary>
    /// Đường dẫn exe để Windows chạy lúc đăng nhập.
    ///
    /// Environment.ProcessPath trả về apphost (Snaptic.App.exe) — đúng thứ cần, kể cả
    /// khi đang chạy qua `dotnet run`. Trả null nếu không xác định được, hoặc nếu tiến
    /// trình là chính dotnet.exe (chạy kiểu `dotnet Snaptic.App.dll`) — đăng ký
    /// dotnet.exe vào Run là vô nghĩa, nó sẽ khởi động mà không biết chạy gì.
    /// </summary>
    private static string? ExecutablePath()
    {
        var path = Environment.ProcessPath;

        if (string.IsNullOrWhiteSpace(path))
            return null;

        var name = Path.GetFileNameWithoutExtension(path);
        if (string.Equals(name, "dotnet", StringComparison.OrdinalIgnoreCase))
            return null;

        return path;
    }
}
