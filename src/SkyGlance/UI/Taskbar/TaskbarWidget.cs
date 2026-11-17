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
    const int WM_MOUSEWHEEL = 0x20A, WM_MOUSEMOVE = 0x200, WM_LBUTTONUP = 0x202, WM_RBUTTONUP = 0x205, WM_MOUSELEAVE = 0x2A3, WM_MOUSEACTIVATE = 0x21;

    public event Action? Clicked;
    /// <summary>True when the mouse enters the widget, false when it leaves.</summary>
    public event Action<bool>? HoverChanged;
    public event Action? RightClicked;
    /// <summary>Mouse wheel over the widget: +1 per notch towards the user (next), -1 away (previous).</summary>
    public event Action<int>? Wheeled;

    IntPtr tray;
    string temp = "--", condition = "", hiLo = "";
    SkyIcon icon = SkyIcon.Cloudy;
    bool hover, rtl, day = true, showCondition = true, showHiLo = true;
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

    // Update progress drawn along the bottom of the pill: busy, the fraction done (null when unknown), the slide phase.
    bool busy;
    double? fraction;
    float phase;
    readonly System.Windows.Forms.Timer anim = new() { Interval = 33 };

    // Extras: a dot for poor air, and rain or snow drifting in the picture.
    Color? badge;
    bool animate, scene;
    float drift, seconds;

    /// <summary>Shows or hides the update progress line. A null fraction slides, for work with no known end.</summary>
    public void SetProgress(bool isBusy, double? done)
    {
        if (isBusy == busy && done == fraction) return;
        busy = isBusy; fraction = done;
        SyncAnim();
        if (Alive) Render();
    }

    /// <param name="airDot">Colour of a dot by the picture when the air is poor, or null for none.</param>
    /// <param name="animated">Animate the picture: rain and snow fall, clouds and fog drift, the sun glows, stars twinkle.</param>
    /// <param name="weatherScene">Paint a small sky for the weather behind the widget instead of the plain pill.</param>
    public void SetExtras(Color? airDot, bool animated, bool weatherScene)
    {
        if (airDot == badge && animated == animate && weatherScene == scene) return;
        badge = airDot; animate = animated; scene = weatherScene;
        SyncAnim();
        if (Alive) Render();
    }

    bool Drifting => animate;

    /// <summary>One timer for both motions: about 30 frames a second for the progress line, 12 for falling rain or snow.</summary>
    void SyncAnim()
    {
        if (anim.Tag == null)
        {
            anim.Tag = true;
            anim.Tick += (_, _) =>
            {
                if (busy && fraction == null) phase = (phase + 0.02f) % 1f;
                // While the progress line slides (30 frames a second) the picture steps at the same speed per second.
                if (Drifting)
                {
                    float share = busy && fraction == null ? 33f / 83f : 1f;
                    drift = (drift + Art.DriftStep(icon) * share) % 1f;
                    seconds += 0.083f * share;
                }
                if (Alive) Render();
            };
        }
        bool sliding = busy && fraction == null;
        anim.Interval = sliding ? 33 : 83;
        anim.Enabled = sliding || Drifting;
        if (!Drifting) drift = 0;
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

    /// <param name="highLow">Today's high and low (such as "↑22° ↓13°"), or null to leave them out.</param>
    public void SetContent(string temperature, string conditionText, string? highLow, SkyIcon sky, bool isDay, bool withCondition, bool isRtl)
    {
        string hl = highLow ?? "";
        if (temperature == temp && conditionText == condition && hl == hiLo && (highLow != null) == showHiLo && sky == icon && isDay == day && withCondition == showCondition && isRtl == rtl) return;
        bool relayout = isRtl != rtl || withCondition != showCondition || (highLow != null) != showHiLo;
        temp = temperature; condition = conditionText; hiLo = hl; showHiLo = highLow != null; icon = sky; day = isDay; showCondition = withCondition; rtl = isRtl;
        SyncAnim(); // the picture may have started or stopped raining
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
        using var bmp = Paint(temp, condition, showHiLo ? hiLo : null, icon, day, showCondition, hover, rtl, SystemDark(), Scale, tr.Height,
                              busy ? new Progress(fraction, phase) : null, badge, Drifting ? drift : -1, scene, Drifting ? seconds : 0);
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
    /// Draws the widget: the weather picture beside the temperature and today's high and low, with the condition below.
    /// The width depends only on the scale and the settings, never on the values, so it doesn't jitter as they change.
    /// </summary>
    /// <summary>An update in progress: the fraction done (null when unknown) and where the sliding segment is.</summary>
    public readonly record struct Progress(double? Fraction, float Phase);

    /// <param name="airDot">Colour of the poor-air dot beside the picture, or null.</param>
    /// <param name="drift">Where falling rain or snow is in the animation (0 to 1), or negative for the still picture.</param>
    public static Bitmap Paint(string temp, string condition, string? highLow, SkyIcon icon, bool isDay, bool withCondition, bool hover, bool rtl, bool dark, float s, int h,
                               Progress? progress = null, Color? airDot = null, float drift = -1, bool scene = false, float seconds = 0)
    {
        // Same sizes as the sibling widgets (Data Tide 13.5 px Semibold, Salaty Reminder 12 px), so they read as one family.
        using var fTemp = new Font("Segoe UI Semibold", (withCondition ? 13.5f : 15f) * s, GraphicsUnit.Pixel);
        using var fCond = new Font("Segoe UI", 12.5f * s, GraphicsUnit.Pixel);
        float tempW, hlW = 0, tempNow;
        using (var probe = new Bitmap(1, 1))
        using (var pg = Graphics.FromImage(probe))
        {
            pg.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
            tempW = pg.MeasureString("-88\u00B0", fTemp).Width;
            tempNow = pg.MeasureString(temp, fTemp).Width;
            // Room for the widest likely high and low, so the width doesn't change with the weather.
            if (highLow != null) hlW = Math.Max(pg.MeasureString("\u219188\u00B0 \u2193-88\u00B0", fCond).Width, pg.MeasureString(highLow, fCond).Width);
        }
        int pad = (int)(9 * s), iconS = (int)Math.Min(h - 14 * s, 30 * s), gap = (int)(7 * s), hlGap = (int)(5 * s);
        float topW = tempW + (highLow != null ? hlGap + hlW : 0);
        int textW = (int)Math.Ceiling(withCondition ? Math.Max(topW, 100 * s) : topW);
        int w = pad + iconS + gap + textW + pad;

        var bmp = new Bitmap(w, h, PixelFormat.Format32bppArgb);
        using (var g = Graphics.FromImage(bmp))
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit; // grayscale AA: correct on per-pixel alpha
            g.Clear(Color.FromArgb(1, 0, 0, 0)); // alpha 1 keeps the whole box clickable

            if (scene)
            {
                // A small sky for the weather: its colours, a faint drifting cloud pattern, and the weather on top
                // (a warm glow behind the sun, stars at night, rain streaks, snowflakes). Lighter on hover.
                var box = new RectangleF(1, 4 * s, w - 2, h - 8 * s);
                using var path = Art.RoundRect(box, 6 * s);
                var (c1, c2) = Art.SceneColors(icon, isDay);
                using (var bg = new LinearGradientBrush(box, Color.FromArgb(240, c1), Color.FromArgb(240, c2), LinearGradientMode.ForwardDiagonal))
                    g.FillPath(bg, path);
                var st = g.Save();
                g.SetClip(path);
                var area = Rectangle.Round(box);
                if (isDay && icon is SkyIcon.Clear or SkyIcon.PartlyCloudy)
                {
                    float gx = rtl ? w - pad - iconS / 2f : pad + iconS / 2f, gy = h / 2f, gr = h * 0.9f;
                    using var glow = new GraphicsPath();
                    glow.AddEllipse(gx - gr, gy - gr, gr * 2, gr * 2);
                    using var gb = new PathGradientBrush(glow) { CenterColor = Color.FromArgb(110, 255, 214, 120), SurroundColors = [Color.FromArgb(0, 255, 214, 120)] };
                    g.FillPath(gb, glow);
                }
                Art.CloudPattern(g, area, 26 * s, Color.FromArgb(16, 255, 255, 255), seconds * 4 * s);
                Art.HeaderWeather(g, area, icon, isDay, seconds, s);
                if (icon == SkyIcon.Fog)
                    using (var mist = new SolidBrush(Color.FromArgb(40, 230, 236, 244)))
                        for (int i = 0; i < 3; i++)
                            g.FillRectangle(mist, area.X, area.Y + area.Height * (0.25f + 0.25f * i) + MathF.Sin(seconds * 0.8f + i) * 2 * s, area.Width, 3 * s);
                if (hover) using (var lift = new SolidBrush(Color.FromArgb(28, 255, 255, 255))) g.FillRectangle(lift, area);
                g.Restore(st);
                using var edge = new Pen(Color.FromArgb(dark ? 60 : 50, 255, 255, 255), Math.Max(1f, s));
                g.DrawPath(edge, path);
            }
            else
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
            Art.Weather(g, new RectangleF(iconX, (h - iconS) / 2f, iconS, iconS), icon, isDay, drift);
            if (airDot is { } dc)
            {
                // A small dot at the picture's top outer corner, ringed so it reads on any picture.
                float r = 3.5f * s, cx = rtl ? iconX + r : iconX + iconS - r, cy = (h - iconS) / 2f + r;
                using var ring = new SolidBrush(dark ? Color.FromArgb(30, 34, 40) : Color.White);
                g.FillEllipse(ring, cx - r - s, cy - r - s, 2 * (r + s), 2 * (r + s));
                using var dot = new SolidBrush(dc);
                g.FillEllipse(dot, cx - r, cy - r, 2 * r, 2 * r);
            }

            // On a weather background the text is always white, like the flyout's header.
            Color fg = scene || dark ? Color.White : Color.FromArgb(20, 20, 20);
            Color dim = scene ? Color.FromArgb(225, 234, 244) : dark ? Color.FromArgb(210, 220, 228) : Color.FromArgb(70, 76, 84);
            int textX = rtl ? pad : pad + iconS + gap;
            using var sf = new StringFormat
            {
                // Near is the reading start: the right edge once DirectionRightToLeft is set.
                Alignment = StringAlignment.Near,
                FormatFlags = StringFormatFlags.NoWrap | (rtl ? StringFormatFlags.DirectionRightToLeft : 0),
                Trimming = StringTrimming.EllipsisCharacter,
            };
            using var tf = new StringFormat(sf) { FormatFlags = StringFormatFlags.NoWrap }; // the number reads left to right
            if (rtl) tf.Alignment = StringAlignment.Far;
            using var b = new SolidBrush(fg);
            using var bd = new SolidBrush(dim);
            float tH = fTemp.GetHeight(g);
            float cH = fCond.GetHeight(g);
            float y1 = withCondition && condition.Length > 0 ? (h - (tH + cH - 4 * s)) / 2 : (h - tH) / 2;
            g.DrawString(temp, fTemp, b, new RectangleF(textX, y1, textW, tH), tf);
            if (!string.IsNullOrEmpty(highLow))
            {
                // Just after the temperature, on the reading side, sharing its baseline.
                float hlY = y1 + (fTemp.FontFamily.GetCellAscent(fTemp.Style) * fTemp.Size / fTemp.FontFamily.GetEmHeight(fTemp.Style))
                               - (fCond.FontFamily.GetCellAscent(fCond.Style) * fCond.Size / fCond.FontFamily.GetEmHeight(fCond.Style));
                float hlX = rtl ? textX + textW - tempNow - hlGap - hlW : textX + tempNow + hlGap;
                using var hf = new StringFormat { Alignment = rtl ? StringAlignment.Far : StringAlignment.Near, FormatFlags = StringFormatFlags.NoWrap };
                g.DrawString(highLow, fCond, bd, new RectangleF(hlX, hlY, hlW + 2 * s, cH), hf);
            }
            if (withCondition && condition.Length > 0)
                g.DrawString(condition, fCond, bd, new RectangleF(textX, y1 + tH - 4 * s, textW, cH), sf);

            if (progress is { } p)
            {
                // A slim line inside the bottom of the pill, like the bar on the Updates page.
                float lh = Math.Max(2f, 2f * s), inset = 7 * s;
                var track = new RectangleF(inset, h - 4 * s - lh - 1.5f * s, w - 2 * inset, lh);
                Color accent = new Theme(dark).Accent;
                Art.FillRound(g, track, lh / 2, Color.FromArgb(dark ? 50 : 40, dark ? Color.White : Color.Black));
                RectangleF fill;
                if (p.Fraction is { } v)
                    fill = new RectangleF(track.X, track.Y, Math.Max(lh, track.Width * (float)Math.Clamp(v, 0, 1)), lh);
                else
                {
                    float seg = track.Width / 3, t = (1 - MathF.Cos(p.Phase * MathF.PI)) / 2;
                    float x = track.X - seg + (track.Width + seg) * t;
                    float l = Math.Max(track.X, x), r = Math.Min(track.Right, x + seg);
                    fill = new RectangleF(l, track.Y, Math.Max(0, r - l), lh);
                }
                if (rtl) fill.X = track.Right - (fill.X - track.X) - fill.Width;
                if (fill.Width >= 1) Art.FillRound(g, fill, lh / 2, accent);
            }
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
            case WM_MOUSEWHEEL:
                int delta = (short)((long)m.WParam >> 16 & 0xFFFF);
                if (delta != 0) Wheeled?.Invoke(delta < 0 ? 1 : -1);
                m.Result = IntPtr.Zero;
                return;
        }
        base.WndProc(ref m);
    }
}
