using System.Runtime.InteropServices;

namespace SkyGlance.Interop;

static class Native
{
    [StructLayout(LayoutKind.Sequential)] public struct RECT { public int Left, Top, Right, Bottom; public int Width => Right - Left; public int Height => Bottom - Top; }
    [StructLayout(LayoutKind.Sequential)] public struct POINT { public int X, Y; }
    [StructLayout(LayoutKind.Sequential)] public struct SIZE { public int CX, CY; }
    [StructLayout(LayoutKind.Sequential, Pack = 1)] public struct BLENDFUNCTION { public byte BlendOp, BlendFlags, SourceConstantAlpha, AlphaFormat; }
    [StructLayout(LayoutKind.Sequential)] public struct TRACKMOUSEEVENT { public int cbSize; public uint dwFlags; public IntPtr hwndTrack; public uint dwHoverTime; }

    public delegate bool EnumProc(IntPtr hwnd, IntPtr lParam);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)] public static extern IntPtr FindWindow(string? cls, string? name);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] public static extern IntPtr FindWindowEx(IntPtr parent, IntPtr after, string? cls, string? name);
    [DllImport("user32.dll")] public static extern bool EnumChildWindows(IntPtr parent, EnumProc cb, IntPtr lParam);
    [DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr hwnd, out RECT r);
    [DllImport("user32.dll")] public static extern bool IsWindow(IntPtr hwnd);
    [DllImport("user32.dll")] public static extern bool IsWindowVisible(IntPtr hwnd);
    [DllImport("user32.dll")] public static extern IntPtr GetParent(IntPtr hwnd);
    [DllImport("user32.dll")] public static extern uint GetWindowThreadProcessId(IntPtr hwnd, out uint pid);
    [DllImport("user32.dll")] public static extern bool SetWindowPos(IntPtr hwnd, IntPtr after, int x, int y, int cx, int cy, uint flags);
    [DllImport("user32.dll")] public static extern uint GetDpiForWindow(IntPtr hwnd);
    [DllImport("user32.dll")] public static extern IntPtr GetDC(IntPtr hwnd);
    [DllImport("user32.dll")] public static extern int ReleaseDC(IntPtr hwnd, IntPtr dc);
    [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr hwnd);
    [DllImport("user32.dll")] public static extern bool TrackMouseEvent(ref TRACKMOUSEEVENT tme);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] public static extern uint RegisterWindowMessage(string msg);
    [DllImport("user32.dll")] public static extern bool AllowSetForegroundWindow(int pid);
    [DllImport("user32.dll")] public static extern bool PostMessage(IntPtr hwnd, uint msg, IntPtr w, IntPtr l);
    [DllImport("user32.dll")] public static extern bool UpdateLayeredWindow(IntPtr hwnd, IntPtr hdcDst, IntPtr pptDst, ref SIZE psize,
        IntPtr hdcSrc, ref POINT pptSrc, int crKey, ref BLENDFUNCTION pblend, int dwFlags);
    [DllImport("gdi32.dll")] public static extern IntPtr CreateCompatibleDC(IntPtr dc);
    [DllImport("gdi32.dll")] public static extern bool DeleteDC(IntPtr dc);
    [DllImport("gdi32.dll")] public static extern IntPtr SelectObject(IntPtr dc, IntPtr obj);
    [DllImport("gdi32.dll")] public static extern bool DeleteObject(IntPtr obj);
    [DllImport("dwmapi.dll")] public static extern int DwmGetWindowAttribute(IntPtr hwnd, int attr, out RECT value, int size);
    [DllImport("dwmapi.dll")] public static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int value, int size);

    [DllImport("uxtheme.dll", CharSet = CharSet.Unicode)] public static extern int SetWindowTheme(IntPtr hwnd, string? app, string? idList);

    /// <summary>Dark scrollbars on a scrollable panel when the app theme is dark.</summary>
    public static void DarkScrollbars(Control c, bool dark)
    {
        if (!dark) return;
        if (c.IsHandleCreated) SetWindowTheme(c.Handle, "DarkMode_Explorer", null);
        else c.HandleCreated += (_, _) => SetWindowTheme(c.Handle, "DarkMode_Explorer", null);
    }

    public const uint SWP_NOSIZE = 0x1, SWP_NOACTIVATE = 0x10, SWP_SHOWWINDOW = 0x40;

    /// <summary>True when Windows uses a dark taskbar / dark system theme.</summary>
    public static bool SystemDark()
    {
        using var k = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
        return k?.GetValue("SystemUsesLightTheme") is not int v || v == 0;
    }

    public static void RoundCorners(IntPtr hwnd, bool dark)
    {
        int round = 2; // DWMWCP_ROUND
        DwmSetWindowAttribute(hwnd, 33, ref round, sizeof(int));
        int d = dark ? 1 : 0;
        DwmSetWindowAttribute(hwnd, 20, ref d, sizeof(int));
    }
}
