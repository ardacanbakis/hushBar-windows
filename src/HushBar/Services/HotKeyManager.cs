using System.Runtime.InteropServices;
using System.Windows.Interop;

namespace HushBar.Services;

public sealed class HotKeyManager : IDisposable
{
    private const int WM_HOTKEY = 0x0312;
    private const int HotKeyId = 0xB00B;

    public const uint MOD_ALT = 1;
    public const uint MOD_CONTROL = 2;
    public const uint MOD_SHIFT = 4;
    public const uint MOD_WIN = 8;
    public const uint MOD_NOREPEAT = 0x4000;

    [DllImport("user32.dll")]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll")]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

    private readonly HwndSource _source;
    private readonly Action _onPressed;
    private uint _currentMods;
    private uint _currentVk;

    public HotKeyManager(Action onPressed, uint modifiers = 0x4006, uint vk = 0x4D)
    {
        _onPressed = onPressed;

        var parameters = new HwndSourceParameters("HushBarHotKeyWindow")
        {
            ParentWindow = new IntPtr(-3),
            Width = 0,
            Height = 0,
        };
        _source = new HwndSource(parameters);
        _source.AddHook(WndProc);

        Register(modifiers, vk);
    }

    public bool Rebind(uint modifiers, uint vk)
    {
        UnregisterHotKey(_source.Handle, HotKeyId);
        return Register(modifiers, vk);
    }

    private bool Register(uint modifiers, uint vk)
    {
        _currentMods = modifiers;
        _currentVk = vk;
        bool ok = RegisterHotKey(_source.Handle, HotKeyId, modifiers | MOD_NOREPEAT, vk);
        if (!ok) HushLog.Write("RegisterHotKey failed (shortcut may be taken)");
        return ok;
    }

    public static string Describe(uint modifiers, uint vk)
    {
        var parts = new List<string>();
        if ((modifiers & MOD_CONTROL) != 0) parts.Add("Ctrl");
        if ((modifiers & MOD_ALT) != 0) parts.Add("Alt");
        if ((modifiers & MOD_SHIFT) != 0) parts.Add("Shift");
        if ((modifiers & MOD_WIN) != 0) parts.Add("Win");
        parts.Add(VkToString(vk));
        return string.Join(" + ", parts);
    }

    private static string VkToString(uint vk) => vk switch
    {
        >= 0x30 and <= 0x39 => ((char)vk).ToString(),
        >= 0x41 and <= 0x5A => ((char)vk).ToString(),
        >= 0x70 and <= 0x87 => $"F{vk - 0x6F}",
        0x20 => "Space",
        0x2E => "Delete",
        0x2D => "Insert",
        0x24 => "Home",
        0x23 => "End",
        0x21 => "PageUp",
        0x22 => "PageDown",
        _ => $"0x{vk:X2}",
    };

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
