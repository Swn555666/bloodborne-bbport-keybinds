// SPDX-License-Identifier: GPL-2.0-or-later
// Copyright (C) 2026 Swn555666
// Authors: Swn555666, Claude (Anthropic)

using System.Diagnostics;
using System.Runtime.InteropServices;

namespace BBKeybinds;

public sealed record GameWindowInfo(IntPtr Handle, string Title, string Process, bool Minimized, bool CoversScreen);

public sealed record HotkeySpec(uint Modifiers, string KeyName)
{
    public const uint ModAlt = 0x0001;
    public const uint ModControl = 0x0002;
    public const uint ModShift = 0x0004;
    public const uint ModWin = 0x0008;

    public static HotkeySpec Default => new(0, "f10");

    public uint Vk => Native.NameToVk(KeyName);

    public string Display
    {
        get
        {
            var parts = new List<string>();
            if ((Modifiers & ModControl) != 0) parts.Add("Ctrl");
            if ((Modifiers & ModShift) != 0) parts.Add("Shift");
            if ((Modifiers & ModAlt) != 0) parts.Add("Alt");
            if ((Modifiers & ModWin) != 0) parts.Add("Win");
            parts.Add(Spec.DisplayName(KeyName));
            return string.Join(" + ", parts);
        }
    }
}

public static class Native
{
    public const int WmHotkey = 0x0312;
    public const int WmApp = 0x8000;
    public const int WmKeyDown = 0x0100;
    public const int WmKeyUp = 0x0101;
    public const int WmSysKeyDown = 0x0104;
    public const int WmSysKeyUp = 0x0105;
    public const uint ModNoRepeat = 0x4000;

    public const int SwHide = 0;
    public const int SwShow = 5;
    public const int SwRestore = 9;

    public const int GwlStyle = -16;
    public const int GwlExStyle = -20;
    public const long WsCaption = 0x00C00000L;
    public const long WsPopup = unchecked((long)0x80000000);
    public const long WsExTopmost = 0x00000008L;
    public const long WsExToolWindow = 0x00000080L;

    public const int SwpNoSize = 0x0001;
    public const int SwpNoMove = 0x0002;
    public const int SwpNoActivate = 0x0010;
    public static readonly IntPtr HwndTopmost = new(-1);
    public static readonly IntPtr HwndNoTopmost = new(-2);
    public static readonly IntPtr HwndBroadcast = new(0xFFFF);

    public const int MonitorDefaultToNearest = 2;

    public const uint InputKeyboard = 1;
    public const uint KeyeventfExtendedKey = 0x0001;
    public const uint KeyeventfKeyUp = 0x0002;
    public const uint KeyeventfScanCode = 0x0008;

    // ---- hotkey ----

    [DllImport("user32.dll", SetLastError = true)]
    public static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll", SetLastError = true)]
    public static extern bool UnregisterHotKey(IntPtr hWnd, int id);

    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    public static extern uint RegisterWindowMessage(string lpString);

    [DllImport("user32.dll", SetLastError = true)]
    public static extern bool PostMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

    // ---- windows ----

    public delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

    [DllImport("user32.dll")]
    public static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    public static extern int GetWindowText(IntPtr hWnd, System.Text.StringBuilder lpString, int nMaxCount);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    public static extern int GetWindowTextLength(IntPtr hWnd);

    [DllImport("user32.dll")]
    public static extern bool IsWindowVisible(IntPtr hWnd);

    [DllImport("user32.dll")]
    public static extern bool IsIconic(IntPtr hWnd);

    [DllImport("user32.dll")]
    public static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

    [DllImport("user32.dll")]
    public static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll", SetLastError = true)]
    public static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    [DllImport("user32.dll", SetLastError = true)]
    public static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

    [DllImport("user32.dll", SetLastError = true)]
    public static extern bool AttachThreadInput(uint idAttach, uint idAttachTo, bool fAttach);

    [DllImport("kernel32.dll")]
    public static extern uint GetCurrentThreadId();

    [DllImport("user32.dll", EntryPoint = "GetWindowLongPtrW", SetLastError = true)]
    public static extern IntPtr GetWindowLongPtr(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll")]
    public static extern short GetAsyncKeyState(int vKey);

    public static bool IsKeyDown(int vk) => (GetAsyncKeyState(vk) & 0x8000) != 0;

    // ---- monitor / fullscreen ----

    [StructLayout(LayoutKind.Sequential)]
    public struct RECT { public int Left, Top, Right, Bottom; }

    [StructLayout(LayoutKind.Sequential)]
    public struct MONITORINFO
    {
        public int cbSize;
        public RECT rcMonitor;
        public RECT rcWork;
        public uint dwFlags;
    }

    [DllImport("user32.dll")]
    public static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

    [DllImport("user32.dll")]
    public static extern IntPtr MonitorFromWindow(IntPtr hwnd, int dwFlags);

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    public static extern bool GetMonitorInfo(IntPtr hMonitor, ref MONITORINFO lpmi);

    public static Rectangle GetMonitorBounds(IntPtr hwnd)
    {
        var hmon = MonitorFromWindow(hwnd, MonitorDefaultToNearest);
        var mi = new MONITORINFO { cbSize = Marshal.SizeOf<MONITORINFO>() };
        if (hmon != IntPtr.Zero && GetMonitorInfo(hmon, ref mi))
            return Rectangle.FromLTRB(mi.rcMonitor.Left, mi.rcMonitor.Top, mi.rcMonitor.Right, mi.rcMonitor.Bottom);
        return Screen.PrimaryScreen?.Bounds ?? new Rectangle(0, 0, 1920, 1080);
    }

    public static bool CoversMonitor(IntPtr hwnd)
    {
        if (!GetWindowRect(hwnd, out var r))
            return false;
        var m = GetMonitorBounds(hwnd);
        long style = GetWindowLongPtr(hwnd, GwlStyle).ToInt64();
        bool hasCaption = (style & WsCaption) == WsCaption;
        return !hasCaption &&
               Math.Abs(r.Left - m.Left) <= 2 && Math.Abs(r.Top - m.Top) <= 2 &&
               Math.Abs(r.Right - m.Right) <= 2 && Math.Abs(r.Bottom - m.Bottom) <= 2;
    }

    // ---- game window ----

    public static GameWindowInfo? FindGameWindow()
    {
        var byProcess = new List<GameWindowInfo>();

        EnumWindows((hwnd, _) =>
        {
            if (!IsWindowVisible(hwnd))
                return true;
            int len = GetWindowTextLength(hwnd);
            if (len <= 0)
                return true;
            var sb = new System.Text.StringBuilder(len + 2);
            GetWindowText(hwnd, sb, sb.Capacity);
            var title = sb.ToString();

            GetWindowThreadProcessId(hwnd, out var pid);
            string proc = "";
            try { proc = Process.GetProcessById((int)pid).ProcessName; }
            catch { /* process ended */ }

            // The game itself is bb-probe.exe. Bloodborne.exe is the desktop
            // launcher, so a window titled "Bloodborne" is not the running game.
            if (proc.Equals("bb-probe", StringComparison.OrdinalIgnoreCase))
            {
                bool minimized = IsIconic(hwnd);
                bool covers = false;
                try { covers = CoversMonitor(hwnd); } catch { }
                byProcess.Add(new GameWindowInfo(hwnd, title, proc, minimized, covers));
            }
            return true;
        }, IntPtr.Zero);

        if (byProcess.Count == 0)
            return null;
        return byProcess.OrderByDescending(w => !w.Minimized).First();
    }

    public static bool ForceForeground(IntPtr hwnd)
    {
        if (hwnd == IntPtr.Zero)
            return false;
        if (GetForegroundWindow() == hwnd)
            return true;

        var fg = GetForegroundWindow();
        uint fgThread = fg == IntPtr.Zero ? 0 : GetWindowThreadProcessId(fg, out _);
        uint myThread = GetCurrentThreadId();
        bool attached = fgThread != 0 && fgThread != myThread && AttachThreadInput(myThread, fgThread, true);
        try
        {
            ShowWindow(hwnd, SwRestore);
            SetForegroundWindow(hwnd);
        }
        finally
        {
            if (attached)
                AttachThreadInput(myThread, fgThread, false);
        }
        return GetForegroundWindow() == hwnd;
    }

    // ---- keyboard send ----

    [StructLayout(LayoutKind.Sequential)]
    public struct KEYBDINPUT
    {
        public ushort wVk;
        public ushort wScan;
        public uint dwFlags;
        public uint time;
        public IntPtr dwExtraInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct MOUSEINPUT
    {
        public int dx, dy;
        public uint mouseData, dwFlags, time;
        public IntPtr dwExtraInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct HARDWAREINPUT
    {
        public uint uMsg;
        public ushort wParamL, wParamH;
    }

    [StructLayout(LayoutKind.Explicit)]
    public struct InputUnion
    {
        [FieldOffset(0)] public MOUSEINPUT mi;
        [FieldOffset(0)] public KEYBDINPUT ki;
        [FieldOffset(0)] public HARDWAREINPUT hi;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct INPUT
    {
        public uint type;
        public InputUnion U;
    }

    [DllImport("user32.dll", SetLastError = true)]
    public static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

    [DllImport("user32.dll")]
    public static extern uint MapVirtualKey(uint uCode, uint uMapType);

    private static bool IsExtendedVk(uint vk) => vk is
        0x21 or 0x22 or 0x23 or 0x24 or 0x25 or 0x26 or 0x27 or 0x28 or // nav cluster / arrows
        0x2C or 0x2D or 0x2E or                                         // print screen, insert, delete
        0x5B or 0x5C or                                                 // win
        0x6F or 0x90 or                                                 // numpad /, numlock
        0xA3 or 0xA5;                                                   // right ctrl, right alt

    public static void SendKey(string keyName)
    {
        uint vk = NameToVk(keyName);
        if (vk == 0)
            return;
        uint scan = MapVirtualKey(vk, 0);
        uint flags = 0;
        if (IsExtendedVk(vk))
            flags |= KeyeventfExtendedKey;

        var inputs = new INPUT[2];
        inputs[0].type = InputKeyboard;
        inputs[0].U.ki = new KEYBDINPUT { wVk = (ushort)vk, wScan = (ushort)scan, dwFlags = flags };
        inputs[1].type = InputKeyboard;
        inputs[1].U.ki = new KEYBDINPUT
        {
            wVk = (ushort)vk,
            wScan = (ushort)scan,
            dwFlags = flags | KeyeventfKeyUp
        };
        SendInput((uint)inputs.Length, inputs, Marshal.SizeOf<INPUT>());
    }

    // ---- key names <-> virtual keys ----

    public static List<string> GetModifierParts()
    {
        var parts = new List<string>();
        if (IsKeyDown(0xA0)) parts.Add("lshift");
        if (IsKeyDown(0xA1)) parts.Add("rshift");
        if (IsKeyDown(0xA2)) parts.Add("lctrl");
        if (IsKeyDown(0xA3)) parts.Add("rctrl");
        if (IsKeyDown(0xA4)) parts.Add("lalt");
        if (IsKeyDown(0xA5)) parts.Add("ralt");
        return parts;
    }

    /// <summary>Maps a raw WM_KEY* message to the port's key name.</summary>
    public static string? VkToName(int vk, long lParam)
    {
        int scan = (int)((lParam >> 16) & 0xFF);
        bool extended = (lParam & (1L << 24)) != 0;

        switch (vk)
        {
            case 0x10: // Shift
                return scan == 0x36 ? "rshift" : "lshift";
            case 0x11: // Ctrl
                return extended ? "rctrl" : "lctrl";
            case 0x12: // Alt
                return extended ? "ralt" : "lalt";
            case 0x5B: return "lwin";
            case 0x5C: return "rwin";
        }

        if (vk >= 0x41 && vk <= 0x5A) return ((char)('a' + vk - 0x41)).ToString();
        if (vk >= 0x30 && vk <= 0x39) return ((char)vk).ToString();
        if (vk >= 0x70 && vk <= 0x7B) return "f" + (vk - 0x6F);
        if (vk >= 0x60 && vk <= 0x69) return "kp" + (vk - 0x60);

        return vk switch
        {
            0x08 => "backspace",
            0x09 => "tab",
            0x0D => extended ? "kpenter" : "enter",
            0x13 => "pausebreak",
            0x14 => "capslock",
            0x1B => "escape",
            0x20 => "space",
            0x21 => "pgup",
            0x22 => "pgdown",
            0x23 => "end",
            0x24 => "home",
            0x25 => "left",
            0x26 => "up",
            0x27 => "right",
            0x28 => "down",
            0x2C => "printscreen",
            0x2D => "insert",
            0x2E => "delete",
            0x6A => "kpasterisk",
            0x6B => "kpplus",
            0x6D => "kpminus",
            0x6E => "kpperiod",
            0x6F => "kpslash",
            0x90 => null, // num lock: the port has no name
            0x91 => "scrolllock",
            0xBA => "semicolon",
            0xBB => "equals",
            0xBC => "comma",
            0xBD => "minus",
            0xBE => "period",
            0xBF => "slash",
            0xC0 => "grave",
            0xDB => "lbracket",
            0xDC => "backslash",
            0xDD => "rbracket",
            0xDE => "apostrophe",
            _ => null
        };
    }

    public static uint NameToVk(string name)
    {
        if (string.IsNullOrEmpty(name)) return 0;
        if (name.Length == 1)
        {
            char c = name[0];
            if (c >= 'a' && c <= 'z') return (uint)('A' + c - 'a');
            if (c >= '0' && c <= '9') return (uint)c;
        }
        if (name.Length == 2 && name[0] == 'k' && char.IsDigit(name[1]))
            return (uint)(0x60 + (name[1] - '0'));
        if (name.StartsWith('f') && int.TryParse(name.AsSpan(1), out var fn) && fn >= 1 && fn <= 12)
            return (uint)(0x6F + fn);
        return name switch
        {
            "escape" => 0x1B,
            "backspace" => 0x08,
            "delete" => 0x2E,
            "insert" => 0x2D,
            "home" => 0x24,
            "end" => 0x23,
            "pgup" => 0x21,
            "pgdown" => 0x22,
            "tab" => 0x09,
            "capslock" => 0x14,
            "enter" => 0x0D,
            "space" => 0x20,
            "up" => 0x26,
            "down" => 0x28,
            "left" => 0x25,
            "right" => 0x27,
            "lshift" => 0xA0,
            "rshift" => 0xA1,
            "lctrl" => 0xA2,
            "rctrl" => 0xA3,
            "lalt" => 0xA4,
            "ralt" => 0xA5,
            "lwin" => 0x5B,
            "rwin" => 0x5C,
            "kpasterisk" => 0x6A,
            "kpplus" => 0x6B,
            "kpminus" => 0x6D,
            "kpperiod" => 0x6E,
            "kpslash" => 0x6F,
            "grave" => 0xC0,
            "minus" => 0xBD,
            "equals" => 0xBB,
            "lbracket" => 0xDB,
            "rbracket" => 0xDD,
            "backslash" => 0xDC,
            "semicolon" => 0xBA,
            "apostrophe" => 0xDE,
            "comma" => 0xBC,
            "period" => 0xBE,
            "slash" => 0xBF,
            "printscreen" => 0x2C,
            "scrolllock" => 0x91,
            "pausebreak" => 0x13,
            "kpenter" => 0x0D,
            _ => 0
        };
    }

    // ---- dark title bar ----

    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

    [DllImport("uxtheme.dll", CharSet = CharSet.Unicode)]
    public static extern int SetWindowTheme(IntPtr hWnd, string pszSubAppName, string? pszSubIdList);

    public static void UseDarkControlTheme(IntPtr hwnd)
    {
        try { SetWindowTheme(hwnd, "DarkMode_Explorer", null); }
        catch { }
    }

    public static void SetDarkTitleBar(IntPtr hwnd, bool dark)
    {
        int value = dark ? 1 : 0;
        int result = DwmSetWindowAttribute(hwnd, 20, ref value, sizeof(int));
        if (result != 0)
            DwmSetWindowAttribute(hwnd, 19, ref value, sizeof(int));
    }
}
