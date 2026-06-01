using System.Runtime.InteropServices;
using System.Windows.Interop;

namespace HushBar.Services;

/// <summary>
/// Global hotkey via Win32 RegisterHotKey. Because HushBar is tray-only with no
/// main window, we host a hidden message-only window to receive WM_HOTKEY.
/// Default shortcut: Ctrl+Shift+M (mirrors the macOS ⇧⌘M).
/// </summary>
public sealed class HotKeyManager : IDisposable
{
    private const int WM_HOTKEY = 0x0312;
    private const int HotKeyId = 0xB00B;

    [Flags]
    private enum Mod : uint { Alt = 1, Control = 2, Shift = 4, Win = 8, NoRepeat = 0x4000 }

    [DllImport("user32.dll")]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll")]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

    private readonly HwndSource _source;
    private readonly Action _onPressed;

    public HotKeyManager(Action onPressed)
    {
        _onPressed = onPressed;

        var parameters = new HwndSourceParameters("HushBarHotKeyWindow")
        {
            // Message-only window (HWND_MESSAGE = -3)
            ParentWindow = new IntPtr(-3),
            Width = 0,
            Height = 0,
        };
        _source = new HwndSource(parameters);
        _source.AddHook(WndProc);

        // Ctrl+Shift+M  (M = 0x4D)
        bool ok = RegisterHotKey(_source.Handle, HotKeyId,
            (uint)(Mod.Control | Mod.Shift | Mod.NoRepeat), 0x4D);
        if (!ok) HushLog.Write("RegisterHotKey failed (shortcut may be taken)");
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == WM_HOTKEY && wParam.ToInt32() == HotKeyId)
        {
            _onPressed();
            handled = true;
        }
        return IntPtr.Zero;
    }

    public void Dispose()
    {
        UnregisterHotKey(_source.Handle, HotKeyId);
        _source.RemoveHook(WndProc);
        _source.Dispose();
    }
}
