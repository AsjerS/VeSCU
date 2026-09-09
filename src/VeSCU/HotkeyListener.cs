using System.Runtime.InteropServices;

namespace VeSCU;

internal sealed partial class HotkeyListener : NativeWindow, IDisposable
{
    private const int WM_HOTKEY = 0x0312;
    private const int HOTKEY_ID = 1;
    private const uint MOD_NOREPEAT = 0x4000;

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool UnregisterHotKey(IntPtr hWnd, int id);

    public event Action? HotkeyPressed;

    public HotkeyListener(AppConfig.HotkeySection hotkey)
    {
        CreateHandle(new CreateParams());

        uint fsModifiers = MOD_NOREPEAT;
        if (hotkey.Alt) fsModifiers |= 0x0001;
        if (hotkey.Ctrl) fsModifiers |= 0x0002;
        if (hotkey.Shift) fsModifiers |= 0x0004;
        if (hotkey.Win) fsModifiers |= 0x0008;

        uint vk = (uint)hotkey.Key;

        if (!RegisterHotKey(Handle, HOTKEY_ID, fsModifiers, vk))
        {
            throw new InvalidOperationException(
                $"Could not register hotkey {hotkey.Key}. It may already be in use."
            );
        }
    }

    protected override void WndProc(ref Message m)
    {
        if (m.Msg == WM_HOTKEY && m.WParam.ToInt32() == HOTKEY_ID)
        {
            HotkeyPressed?.Invoke();
        }
        base.WndProc(ref m);
    }

    public void Dispose()
    {
        UnregisterHotKey(this.Handle, HOTKEY_ID);
        DestroyHandle();
    }
}