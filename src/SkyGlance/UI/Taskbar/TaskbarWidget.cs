using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;
using static SkyGlance.Interop.Native;

namespace SkyGlance.UI.Taskbar;

/// <summary>
/// A per-pixel-alpha child window inside the Windows taskbar (Shell_TrayWnd) showing the weather picture,
/// the temperature and the condition.
/// It sits left of the notification area and takes the nearest free spot that overlaps no other app's window
/// embedded there (Data Tide, Salaty Reminder, TrafficMonitor...). Being a child of the taskbar, it hides with it in fullscreen.
/// </summary>
sealed class TaskbarWidget : NativeWindow
{
    const int WS_CHILD = 0x40000000, WS_CLIPSIBLINGS = 0x04000000;
    const int WS_EX_LAYERED = 0x80000, WS_EX_TOOLWINDOW = 0x80, WS_EX_NOACTIVATE = 0x08000000;
    const int WM_MOUSEMOVE = 0x200, WM_LBUTTONUP = 0x202, WM_RBUTTONUP = 0x205, WM_MOUSELEAVE = 0x2A3, WM_MOUSEACTIVATE = 0x21;

    public event Action? Clicked;
    /// <summary>True when the mouse enters the widget, false when it leaves.</summary>
    public event Action<bool>? HoverChanged;
    public event Action? RightClicked;

    IntPtr tray;
    string temp = "--", condition = "";
    SkyIcon icon = SkyIcon.Cloudy;
    bool hover, rtl, day = true, showCondition = true;
    Size size;
    Point lastPos = new(int.MinValue, 0);

    /// <summary>Left end of the taskbar instead of beside the tray.</summary>
    public bool LeftSide { get; private set; }
    /// <summary>Shift towards the taskbar centre, in pixels at 100% scale.</summary>
    public int Offset { get; private set; }

    public void SetPlacement(bool left, int offset)
    {
        if (left == LeftSide && offset == Offset) return;
        LeftSide = left;
        Offset = offset;
        lastPos = new(int.MinValue, 0);
        if (Alive) Place();
    }

    public bool Alive => Handle != IntPtr.Zero && IsWindow(Handle);

    public Rectangle? ScreenBounds => Alive && GetWindowRect(Handle, out var r)
        ? Rectangle.FromLTRB(r.Left, r.Top, r.Right, r.Bottom) : null;

    /// <summary>Creates the window if needed (first run, Explorer restart) and keeps it positioned.</summary>
    public void Ensure()
    {
        var t = FindWindow("Shell_TrayWnd", null);
        if (t == IntPtr.Zero) return;
        if (Handle != IntPtr.Zero && (t != tray || !IsWindow(Handle)))
        {
            try { DestroyHandle(); } catch { ReleaseHandle(); }
        }
        if (Handle == IntPtr.Zero)
        {
            tray = t;
            lastPos = new(int.MinValue, 0);
            CreateHandle(new CreateParams
            {
                Caption = "SkyGlanceTaskbarWindow",
                Parent = t,
                Style = WS_CHILD | WS_CLIPSIBLINGS,
                ExStyle = WS_EX_LAYERED | WS_EX_TOOLWINDOW | WS_EX_NOACTIVATE,
                Width = 1, Height = 1,
            });
            Render();
        }
        Place();
    }

    public void SetContent(string temperature, string conditionText, SkyIcon sky, bool isDay, bool withCondition, bool isRtl)
    {
        if (temperature == temp && conditionText == condition && sky == icon && isDay == day && withCondition == showCondition && isRtl == rtl) return;
        bool relayout = isRtl != rtl || withCondition != showCondition;
        temp = temperature; condition = conditionText; icon = sky; day = isDay; showCondition = withCondition; rtl = isRtl;
        if (!Alive) return;
        var before = size;
        Render();
        if (relayout || size != before) Place();
    }

    public void Invalidate() { if (Alive) { Render(); Place(); } }

    float Scale => tray != IntPtr.Zero && GetDpiForWindow(tray) is var d and > 0 ? d / 96f : 1f;

    void Place()
    {
        if (!GetWindowRect(tray, out var tr)) return;
        int gap = (int)(6 * Scale), shift = (int)(Offset * Scale);
        int y = Math.Max(0, (tr.Height - size.Height) / 2);
        Point pos;
        if (LeftSide)
        {
            int left = FreeLeftEdge(tr, tr.Left + gap + shift, gap);
            pos = new Point(Math.Clamp(left - tr.Left, 0, Math.Max(0, tr.Width - size.Width)), y);
        }
        else
        {
            int right = FreeRightEdge(tr, TrayLeftEdge(tr) - shift, gap);
            pos = new Point(Math.Max(0, right - size.Width - tr.Left), y);
            if (IsWin10) MakeRoomOnWin10(tr, pos.X + tr.Left - gap);
        }
        // Compare with where the window really is, so anything that moved it gets corrected.
        if (pos == lastPos && IsWindowVisible(Handle) && GetWindowRect(Handle, out var cur)
            && cur.Left - tr.Left == pos.X && cur.Top - tr.Top == pos.Y) return;
        lastPos = pos;
        SetWindowPos(Handle, IntPtr.Zero, pos.X, pos.Y, 0, 0, SWP_NOSIZE | SWP_NOACTIVATE | SWP_SHOWWINDOW);
    }

    /// <summary>Other apps' visible windows embedded in the taskbar (Explorer's own and ours are skipped).</summary>
    List<RECT> OtherWidgets(Func<RECT, bool> keep)
    {
        var others = new List<RECT>();
        GetWindowThreadProcessId(tray, out uint explorerPid);
        EnumChildWindows(tray, (h, _) =>
        {
            if (h == Handle || !IsWindowVisible(h)) return true;
            GetWindowThreadProcessId(h, out uint pid);
            if (pid == explorerPid || pid == Environment.ProcessId) return true;
            if (GetWindowRect(h, out var r) && r.Width > 0 && keep(r)) others.Add(r);
            return true;
        }, IntPtr.Zero);
        return others;
    }

    /// <summary>
    /// Screen x where our left edge goes on the left side: the nearest spot right of <paramref name="anchor"/>
    /// that overlaps no other app's window embedded in the left half of the taskbar (Salaty Reminder, Data Tide...).
    /// </summary>
    int FreeLeftEdge(RECT tr, int anchor, int gap)
    {
        int mid = tr.Left + tr.Width / 2;
        var others = OtherWidgets(r => r.Right <= mid);
        int left = anchor;
        for (int pass = 0; pass < 12; pass++)
        {
            int right = left + size.Width;
            var hit = others.Where(r => r.Left < right + gap && r.Right > left - gap).ToList();
            if (hit.Count == 0) break;
            left = hit.Max(r => r.Right) + gap;
        }
        return left;
    }

    /// <summary>
    /// Screen x where our right edge goes: the nearest spot left of <paramref name="anchor"/> that overlaps
    /// no other app's window embedded in the right half of the taskbar. Only windows that would overlap us
    /// push us left, so two widgets that each avoid the other settle instead of chasing each other.
    /// </summary>
    int FreeRightEdge(RECT tr, int anchor, int gap)
    {
        int mid = tr.Left + tr.Width / 2;
        var others = OtherWidgets(r => r.Left > mid);

        int right = anchor - gap;
        for (int pass = 0; pass < 12; pass++)
        {
            int left = right - size.Width;
            var hit = others.Where(r => r.Left < right + gap && r.Right > left - gap).ToList();
            if (hit.Count == 0) break;
            right = hit.Min(r => r.Left) - gap;
        }
        return right;
    }

    volatile int uiaTrayLeft;   // last tray edge found through UI Automation
    volatile bool uiaBusy;
    DateTime uiaCheckedAt;

    /// <summary>
    /// Screen x where the notification area starts. TrayNotifyWnd is right when it has real bounds,
    /// but on some Windows 11 builds it stays 0 px wide at x=0 after boot. Then ask UI Automation
    /// for the leftmost tray button.
    /// </summary>
    int TrayLeftEdge(RECT tr)
    {
        var notify = FindWindowEx(tray, IntPtr.Zero, "TrayNotifyWnd", null);
        int mid = tr.Left + tr.Width / 2;
        if (notify != IntPtr.Zero && GetWindowRect(notify, out var nr) && nr.Width > 0 && nr.Left > mid && nr.Right <= tr.Right + 2)
            return nr.Left;

        // UI Automation runs on a background thread: this window is inside the taskbar, so querying
        // the taskbar from our own UI thread would call back into us and could deadlock.
        if (!uiaBusy && (DateTime.Now - uiaCheckedAt).TotalSeconds >= (uiaTrayLeft > mid ? 5 : 1))
        {
            uiaBusy = true;
            uiaCheckedAt = DateTime.Now;
            var taskbar = tray;
            Task.Run(() =>
            {
                int found = UiaTrayLeft(taskbar, mid);
                if (found > mid) uiaTrayLeft = found;
                uiaBusy = false;
            });
        }
        return uiaTrayLeft > mid ? uiaTrayLeft : tr.Right - (int)(300 * Scale); // until UIA answers: typical tray width
    }

    static int UiaTrayLeft(IntPtr taskbar, int mid)
    {
        try
        {
            var root = System.Windows.Automation.AutomationElement.FromHandle(taskbar);
            var cond = new System.Windows.Automation.PropertyCondition(
                System.Windows.Automation.AutomationElement.AutomationIdProperty, "SystemTrayIcon");
            int left = int.MaxValue;
            foreach (System.Windows.Automation.AutomationElement e in root.FindAll(System.Windows.Automation.TreeScope.Descendants, cond))
            {
                var r = e.Current.BoundingRectangle;
                if (r.Width > 0 && r.X > mid) left = Math.Min(left, (int)r.X);
            }
            return left == int.MaxValue ? 0 : left;
        }
        catch (Exception ex)
        {
            Log.Error(ex);
            return 0;
        }
    }

    static readonly bool IsWin10 = Environment.OSVersion.Version.Build < 22000;

    /// <summary>
    /// Windows 10 task buttons run from the Start button to the tray, so shrink their strip
    /// (ReBarWindow32) to end before the widget. Explorer re-lays it out by itself when we exit.
    /// </summary>
    void MakeRoomOnWin10(RECT taskbar, int screenRight)
    {
        var rebar = FindWindowEx(tray, IntPtr.Zero, "ReBarWindow32", null);
        if (rebar == IntPtr.Zero || !GetWindowRect(rebar, out var rr)) return;
        if (taskbar.Height > taskbar.Width) return; // vertical taskbar: leave it alone
        if (rr.Right <= screenRight) return;
        int width = Math.Max(0, screenRight - rr.Left);
        SetWindowPos(rebar, IntPtr.Zero, rr.Left - taskbar.Left, rr.Top - taskbar.Top, width, rr.Height, SWP_NOACTIVATE | 0x4 /* NOZORDER */);
    }

    void Render()
    {
        if (!GetWindowRect(tray, out var tr)) return;
        using var bmp = Paint(temp, condition, icon, day, showCondition, hover, rtl, SystemDark(), Scale, tr.Height);
        size = bmp.Size;
        var screen = GetDC(IntPtr.Zero);
        var mem = CreateCompatibleDC(screen);
        var hbmp = bmp.GetHbitmap(Color.FromArgb(0));
        var old = SelectObject(mem, hbmp);
        try
        {
            var sz = new SIZE { CX = size.Width, CY = size.Height };
            var src = new POINT();
            var blend = new BLENDFUNCTION { BlendOp = 0, SourceConstantAlpha = 255, AlphaFormat = 1 };
            UpdateLayeredWindow(Handle, screen, IntPtr.Zero, ref sz, mem, ref src, 0, ref blend, 2 /* ULW_ALPHA */);
        }
        finally
        {
            SelectObject(mem, old);
            DeleteObject(hbmp);
            DeleteDC(mem);
            ReleaseDC(IntPtr.Zero, screen);
        }
    }

    /// <summary>
    /// Draws the widget: the weather picture beside the temperature, with the condition below it.
    /// The width depends only on the scale and the settings, never on the values, so it doesn't jitter as they change.
    /// </summary>
    public static Bitmap Paint(string temp, string condition, SkyIcon icon, bool isDay, bool withCondition, bool hover, bool rtl, bool dark, float s, int h)
    {
        using var fTemp = new Font("Segoe UI Semibold", (withCondition ? 13f : 15f) * s, GraphicsUnit.Pixel);
        using var fCond = new Font("Segoe UI", 11f * s, GraphicsUnit.Pixel);
        float tempW;
        using (var probe = new Bitmap(1, 1))
        using (var pg = Graphics.FromImage(probe))
        {
            pg.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
            tempW = pg.MeasureString("-88°", fTemp).Width;
        }
        int pad = (int)(8 * s), iconS = (int)Math.Min(h - 14 * s, 30 * s), gap = (int)(6 * s);
        int textW = (int)Math.Ceiling(withCondition ? Math.Max(tempW, 92 * s) : tempW);
        int w = pad + iconS + gap + textW + pad;

        var bmp = new Bitmap(w, h, PixelFormat.Format32bppArgb);
        using (var g = Graphics.FromImage(bmp))
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit; // grayscale AA: correct on per-pixel alpha
            g.Clear(Color.FromArgb(1, 0, 0, 0)); // alpha 1 keeps the whole box clickable

            // Soft sky-blue pill like the other widgets, brighter on hover.
            {
                var box = new RectangleF(1, 4 * s, w - 2, h - 8 * s);
                int a1 = hover ? (dark ? 95 : 70) : (dark ? 55 : 40);
                int a2 = hover ? (dark ? 70 : 50) : (dark ? 30 : 22);
                using var path = Art.RoundRect(box, 6 * s);
                using var bg = new LinearGradientBrush(box, Color.FromArgb(a1, Art.SkyLight), Color.FromArgb(a2, Art.SkyDark), LinearGradientMode.ForwardDiagonal);
                g.FillPath(bg, path);
                using var edge = new Pen(Color.FromArgb(dark ? 40 : 30, dark ? Color.White : Art.SkyDark), Math.Max(1f, s));
                g.DrawPath(edge, path);
            }

            int iconX = rtl ? w - pad - iconS : pad;
            Art.Weather(g, new RectangleF(iconX, (h - iconS) / 2f, iconS, iconS), icon, isDay);

            Color fg = dark ? Color.White : Color.FromArgb(20, 20, 20);
            Color dim = dark ? Color.FromArgb(210, 220, 228) : Color.FromArgb(70, 76, 84);
            int textX = rtl ? pad : pad + iconS + gap;
            using var sf = new StringFormat
            {
                Alignment = rtl ? StringAlignment.Far : StringAlignment.Near,
                FormatFlags = StringFormatFlags.NoWrap | (rtl ? StringFormatFlags.DirectionRightToLeft : 0),
                Trimming = StringTrimming.EllipsisCharacter,
            };
            using var tf = new StringFormat(sf) { FormatFlags = StringFormatFlags.NoWrap }; // the number reads left to right
            if (rtl) tf.Alignment = StringAlignment.Far;
            using var b = new SolidBrush(fg);
            using var bd = new SolidBrush(dim);
            float tH = fTemp.GetHeight(g);
            if (withCondition && condition.Length > 0)
            {
                float cH = fCond.GetHeight(g);
                float total = tH + cH - 3 * s;
                float y1 = (h - total) / 2;
                g.DrawString(temp, fTemp, b, new RectangleF(textX, y1, textW, tH), tf);
                g.DrawString(condition, fCond, bd, new RectangleF(textX, y1 + tH - 3 * s, textW, cH), sf);
            }
            else
                g.DrawString(temp, fTemp, b, new RectangleF(textX, (h - tH) / 2, textW, tH), tf);
        }
        return bmp;
    }

    protected override void WndProc(ref Message m)
    {
        switch (m.Msg)
        {
            case WM_MOUSEACTIVATE:
                m.Result = 3; // MA_NOACTIVATE
                return;
            case WM_MOUSEMOVE when !hover:
                hover = true;
                HoverChanged?.Invoke(true);
                var tme = new TRACKMOUSEEVENT { cbSize = System.Runtime.InteropServices.Marshal.SizeOf<TRACKMOUSEEVENT>(), dwFlags = 2 /* TME_LEAVE */, hwndTrack = Handle };
                TrackMouseEvent(ref tme);
                Render();
                break;
            case WM_MOUSELEAVE:
                hover = false;
                HoverChanged?.Invoke(false);
                Render();
                break;
            case WM_LBUTTONUP:
                Clicked?.Invoke();
                return;
            case WM_RBUTTONUP:
                RightClicked?.Invoke();
                return;
        }
        base.WndProc(ref m);
    }
}
