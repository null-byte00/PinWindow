using System.Windows;
using System.Windows.Interop;
using Microsoft.Win32;

namespace PinWindow;

public partial class App : System.Windows.Application
{
    sealed class Pin { public bool Ghost; public long ExStyle; }

    const int PinId = 1, GhostId = 2;

    Settings _s = null!;
    readonly Dictionary<IntPtr, Pin> _pinned = new();
    System.Windows.Forms.NotifyIcon? _tray;
    System.Windows.Forms.ToolStripMenuItem _pinList = null!;
    HwndSource? _src;
    static Mutex? _mutex;

    protected override void OnStartup(StartupEventArgs e)
    {
        _mutex = new Mutex(true, "PinWindow_single", out bool fresh);
        if (!fresh) { Shutdown(); return; }

        _s = Settings.Load();

        // невидимое окно для хоткеев
        var p = new HwndSourceParameters("pinw_msg") { WindowStyle = 0x800000 };
        _src = new HwndSource(p);
        _src.AddHook(WndProc);

        SetupTray();
        Reregister();
        Toast("PinWindow живёт в трее — " + _s.PinHotkey + " пин окна, " + _s.GhostHotkey + " сквозной клик");
        base.OnStartup(e);
    }

    void Reregister()
    {
        if (_src == null) return;
        Hotkey.Register(_src.Handle, _s.PinHotkey, PinId);
        Hotkey.Register(_src.Handle, _s.GhostHotkey, GhostId);
    }

    IntPtr WndProc(IntPtr hwnd, int msg, IntPtr w, IntPtr l, ref bool handled)
    {
        if (msg != Native.WM_HOTKEY) return IntPtr.Zero;
        handled = true;
        if (w.ToInt32() == PinId) TogglePin();
        else if (w.ToInt32() == GhostId) ToggleGhost();
        return IntPtr.Zero;
    }

    void TogglePin()
    {
        var hwnd = Native.GetForegroundWindow();
        if (hwnd == IntPtr.Zero || hwnd == _src!.Handle) return;

        if (_pinned.Remove(hwnd, out _))
        {
            Native.SetWindowPos(hwnd, Native.HWND_NOTOPMOST, 0, 0, 0, 0,
                Native.SWP_NOMOVE | Native.SWP_NOSIZE | Native.SWP_NOACTIVATE);
            Toast("«" + Native.TitleOf(hwnd) + "» откреплено");
        }
        else
        {
            _pinned[hwnd] = new Pin { ExStyle = Native.GetExStyle(hwnd) };
            Native.SetWindowPos(hwnd, Native.HWND_TOPMOST, 0, 0, 0, 0,
                Native.SWP_NOMOVE | Native.SWP_NOSIZE | Native.SWP_NOACTIVATE);
            Toast("«" + Native.TitleOf(hwnd) + "» поверх всех");
        }
        RefreshTray();
    }

    void ToggleGhost()
    {
        // сквозной клик у последнего закреплённого (или у форграунд-закреплённого)
        var fg = Native.GetForegroundWindow();
        var hwnd = _pinned.ContainsKey(fg) ? fg
            : _pinned.Keys.LastOrDefault(Native.IsWindow);
        if (hwnd == IntPtr.Zero || !_pinned.TryGetValue(hwnd, out var p))
        { Toast("Нечего: сначала запини окно"); return; }

        p.Ghost = !p.Ghost;
        var ex = Native.GetExStyle(hwnd);
        if (p.Ghost)
            Native.SetExStyle(hwnd, ex | Native.WS_EX_LAYERED | Native.WS_EX_TRANSPARENT | Native.WS_EX_NOACTIVATE);
        else
            Native.SetExStyle(hwnd, ex & ~Native.WS_EX_TRANSPARENT & ~Native.WS_EX_NOACTIVATE);
        Toast("«" + Native.TitleOf(hwnd) + "» " + (p.Ghost ? "клики проходят насквозь" : "клики снова обычные"));
    }

    void UnpinAll()
    {
        foreach (var (hwnd, p) in _pinned)
            if (Native.IsWindow(hwnd))
            {
                Native.SetWindowPos(hwnd, Native.HWND_NOTOPMOST, 0, 0, 0, 0,
                    Native.SWP_NOMOVE | Native.SWP_NOSIZE | Native.SWP_NOACTIVATE);
                if (p.Ghost)
                    Native.SetExStyle(hwnd, Native.GetExStyle(hwnd) & ~Native.WS_EX_TRANSPARENT & ~Native.WS_EX_NOACTIVATE);
            }
        _pinned.Clear();
        RefreshTray();
        Toast("Все окна откреплены");
    }

    void SetupTray()
    {
        _tray = new System.Windows.Forms.NotifyIcon
        {
            Icon = System.Drawing.SystemIcons.WinLogo,
            Text = "PinWindow",
            Visible = true,
            ContextMenuStrip = new System.Windows.Forms.ContextMenuStrip()
        };
        var menu = _tray.ContextMenuStrip;
        _pinList = new System.Windows.Forms.ToolStripMenuItem("Закреплённые: нет") { Enabled = false };
        menu.Items.Add(_pinList);
        menu.Items.Add(new System.Windows.Forms.ToolStripSeparator());
        menu.Items.Add("Открепить все", null, (_, _) => UnpinAll());
        var autostart = new System.Windows.Forms.ToolStripMenuItem("С Windows") { Checked = _s.StartWithWindows };
        autostart.Click += (_, _) =>
        {
            _s.StartWithWindows = !_s.StartWithWindows;
            _s.Save();
            autostart.Checked = _s.StartWithWindows;
            SetAutostart(_s.StartWithWindows);
        };
        menu.Items.Add(autostart);
        menu.Items.Add("Выход", null, (_, _) => { UnpinAll(); _tray.Visible = false; Shutdown(); });
    }

    void RefreshTray()
    {
        // выкинуть мёртвые hwnd
        foreach (var dead in _pinned.Keys.Where(h => !Native.IsWindow(h)).ToList())
            _pinned.Remove(dead);

        _pinList.DropDownItems.Clear();
        if (_pinned.Count == 0)
        {
            _pinList.Text = "Закреплённые: нет";
            _pinList.Enabled = false;
            return;
        }
        _pinList.Text = "Закреплённые: " + _pinned.Count;
        _pinList.Enabled = true;
        foreach (var hwnd in _pinned.Keys)
        {
            var h = hwnd;
            _pinList.DropDownItems.Add("«" + Native.TitleOf(h) + "» — открепить", null,
                (_, _) => { _pinned.Remove(h); Native.SetWindowPos(h, Native.HWND_NOTOPMOST, 0, 0, 0, 0, Native.SWP_NOMOVE | Native.SWP_NOSIZE | Native.SWP_NOACTIVATE); RefreshTray(); });
        }
    }

    void Toast(string text) => _tray?.ShowBalloonTip(1500, "PinWindow", text, System.Windows.Forms.ToolTipIcon.Info);

    public static void SetAutostart(bool on)
    {
        using var key = Registry.CurrentUser.OpenSubKey(
            @"Software\Microsoft\Windows\CurrentVersion\Run", true);
        if (on) key?.SetValue("PinWindow", '"' + Environment.ProcessPath + '"');
        else key?.DeleteValue("PinWindow", false);
    }
}
