using Microsoft.Win32;

namespace VeSCU;

internal static class AutoStartupManager
{
    private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string ValueName = "VeSCU";

    public static bool IsEnabled()
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: false);
        return key?.GetValue(ValueName) is not null;
    }

    public static void SetEnabled(bool enable)
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: true);
        
        if (enable)
            key?.SetValue(ValueName, $"\"{Environment.ProcessPath}\"");
        else
            key?.DeleteValue(ValueName, throwOnMissingValue: false);
    }
}
