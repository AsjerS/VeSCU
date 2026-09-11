using Microsoft.Win32;

namespace VeSCU;

internal static class AutoStartupManager
{
    private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";

    public static bool IsEnabled()
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: false);
        return key?.GetValue(AppInfo.Name) is not null;
    }

    public static void SetEnabled(bool enable)
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: true);
        
        if (enable)
            key?.SetValue(AppInfo.Name, $"\"{Environment.ProcessPath}\"");
        else
            key?.DeleteValue(AppInfo.Name, throwOnMissingValue: false);
    }
}
