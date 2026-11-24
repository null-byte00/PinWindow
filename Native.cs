using System.Runtime.InteropServices;
using System.Text;

namespace PinWindow;

public static class Native
{
    [DllImport("user32.dll")] public static extern IntPtr GetForegroundWindow();
    [DllImport("user32.dll")] public static extern bool IsWindow(IntPtr hwnd);
    [DllImport("user32.dll")] static extern int GetWindowText(IntPtr hwnd, StringBuilder sb, int max);
    [DllImport("user32.dll")] public static extern int GetWindowTextLength(IntPtr hwnd);
    [DllImport("user32.dll")] public static extern uint GetWindowThreadProcessId(IntPtr hwnd, out uint pid);
    [DllImport("user32.dll")] static extern IntPtr GetWindowLongPtr(IntPtr hwnd, int idx);
    [DllImport("user32.dll")] static extern IntPtr SetWindowLongPtr(IntPtr hwnd, int idx, IntPtr val);
    [DllImport("user32.dll")] public static extern bool SetWindowPos(IntPtr hwnd, IntPtr insAfter,
        int x, int y, int cx, int cy, uint flags);
    [DllImport("user32.dll")] public static extern bool RegisterHotKey(IntPtr hwnd, int id, uint mods, uint vk);
    [DllImport("user32.dll")] public static extern bool UnregisterHotKey(IntPtr hwnd, int id);
    [DllImport("user32.dll")] static extern int GetWindowLong(IntPtr hwnd, int idx);
    [DllImport("user32.dll")] static extern int SetWindowLong(IntPtr hwnd, int idx, int val);

    public static readonly IntPtr HWND_TOPMOST = new(-1);
    public static readonly IntPtr HWND_NOTOPMOST = new(-2);
    public const uint SWP_NOMOVE = 0x2, SWP_NOSIZE = 0x1, SWP_NOACTIVATE = 0x10;
    public const int GWL_EXSTYLE = -20;
    public const long WS_EX_TOPMOST = 0x8;
    public const long WS_EX_LAYERED = 0x80000;
    public const long WS_EX_TRANSPARENT = 0x20;
    public const long WS_EX_NOACTIVATE = 0x8000000;
    public const int WM_HOTKEY = 0x312;
    public const uint MOD_ALT = 0x1, MOD_CTRL = 0x2, MOD_SHIFT = 0x4, MOD_WIN = 0x8, MOD_NOREPEAT = 0x4000;

    public static long GetExStyle(IntPtr hwnd) =>
        IntPtr.Size == 8 ? GetWindowLongPtr(hwnd, GWL_EXSTYLE).ToInt64() : GetWindowLong(hwnd, GWL_EXSTYLE);

    public static void SetExStyle(IntPtr hwnd, long val)
    {
        if (IntPtr.Size == 8) SetWindowLongPtr(hwnd, GWL_EXSTYLE, new IntPtr(val));
        else SetWindowLong(hwnd, GWL_EXSTYLE, (int)val);
    }

    public static string TitleOf(IntPtr hwnd)
    {
        var sb = new StringBuilder(256);
        GetWindowText(hwnd, sb, sb.Capacity);
        var t = sb.ToString().Trim();
        return t.Length > 40 ? t[..40] + "…" : t;
    }
}
