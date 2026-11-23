using Microsoft.Win32;

namespace AlertSpeaker.Config;

/// <summary>
/// 当前用户开机启动登记（HKCU Run），不需要管理员权限。
/// </summary>
internal static class WindowsStartup
{
    public const string ValueName = "alert_speaker";

    private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";

    public static string? ResolveExePath()
    {
        var path = Environment.ProcessPath;
        if (string.IsNullOrWhiteSpace(path))
            path = Application.ExecutablePath;
        if (string.IsNullOrWhiteSpace(path))
            return null;
        return Path.GetFullPath(path);
    }

    public static void Apply(bool enabled)
    {
        if (enabled)
            Register();
        else
            Unregister();
    }

    public static void Register()
    {
        var exe = ResolveExePath();
        if (string.IsNullOrWhiteSpace(exe) || !File.Exists(exe))
            throw new InvalidOperationException("无法确定当前可执行文件路径，未能登记开机启动。");

        var command = "\"" + exe + "\"";
        using var key = Registry.CurrentUser.CreateSubKey(RunKeyPath, writable: true)
            ?? throw new InvalidOperationException("无法打开当前用户的开机启动注册表项。");
        key.SetValue(ValueName, command, RegistryValueKind.String);
    }

    public static void Unregister()
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: true);
        key?.DeleteValue(ValueName, throwOnMissingValue: false);
    }
}
