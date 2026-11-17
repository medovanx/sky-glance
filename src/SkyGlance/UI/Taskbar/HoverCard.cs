using System.Drawing.Drawing2D;

namespace SkyGlance.UI.Taskbar;

/// <summary>
/// A small card above the widget after the mouse rests on it, like Data Tide's: the place, rain about to start
/// or stop, the next six hours, and the wind and air. It never takes focus and hides as soon as the mouse leaves.
/// </summary>
sealed class HoverCard : Form
{
    const int W = 300, H = 148, Hours = 6;

    readonly System.Windows.Forms.Timer delay = new() { Interval = 450 };
    SkyGlanceApp? app;
    Rectangle? anchor;
    Func<bool>? suppressed;
    bool dark;

    public HoverCard()
    {
        FormBorderStyle = FormBorderStyle.None;
        ShowInTaskbar = false;
        TopMost = true;
        StartPosition = FormStartPosition.Manual;
        DoubleBuffered = true;
        delay.Tick += (_, _) => { delay.Stop(); ShowNow(); };
    }

    protected override bool ShowWithoutActivation => true;

    protected override CreateParams CreateParams
    {
        get
        {
            var cp = base.CreateParams;
            cp.ClassStyle |= 0x20000;               // CS_DROPSHADOW
            cp.ExStyle |= 0x80 | 0x08000000 | 0x20; // TOOLWINDOW | NOACTIVATE | TRANSPARENT (clicks pass through)
            return cp;
        }
    }

    float S => DeviceDpi / 96f;
    int Px(float v) => (int)Math.Round(v * S);
    bool Rtl => L.Ar;
    Rectangle M(Rectangle r) => Rtl ? new Rectangle(Width - r.Right, r.Y, r.Width, r.Height) : r;
    RectangleF M(RectangleF r) => Rtl ? new RectangleF(Width - r.Right, r.Y, r.Width, r.Height) : r;
    TextFormatFlags Near => Rtl ? TextFormatFlags.Right | TextFormatFlags.RightToLeft : TextFormatFlags.Left;
    TextFormatFlags Far => Rtl ? TextFormatFlags.Left | TextFormatFlags.RightToLeft : TextFormatFlags.Right;

    /// <param name="suppress">Checked again when the delay ends: the card must never cover the open flyout or menu.</param>
    public void ShowSoon(SkyGlanceApp owner, Rectangle? widget, Func<bool>? suppress = null)
    {
        app = owner;
        anchor = widget;
        suppressed = suppress;
        delay.Stop();
        delay.Start();
    }

    public void Cancel()
    {
        delay.Stop();
        if (Visible) Hide();
    }

    /// <summary>Lays the card out off-screen for --shots.</summary>
    public void ShowForShot(SkyGlanceApp owner, bool darkTheme)
    {
        app = owner;
        dark = darkTheme;
        Prepare();
        Location = new Point(-4000, -4000);
        Show();
    }

    void Prepare()
    {
        RightToLeft = Rtl ? RightToLeft.Yes : RightToLeft.No;
        BackColor = new Theme(dark).Surface;
        Size = new Size(Px(W), Px(H));
    }

    void ShowNow()
    {
        if (app?.Forecast == null || anchor is not { } a || suppressed?.Invoke() == true) return;
        dark = Native.SystemDark();
        Prepare();
        var wa = Screen.FromRectangle(a).WorkingArea;
        int x = Math.Clamp(a.Left + a.Width / 2 - Width / 2, wa.Left + Px(6), wa.Right - Width - Px(6));
        Location = new Point(x, wa.Bottom - Height - Px(8));
        Invalidate();
        // SW_SHOWNOACTIVATE: appear without taking focus, so whatever is active stays active.
        Native.ShowWindow(Handle, 4);
        Visible = true;
        Native.RoundCorners(Handle, dark);
    }

    string Num(double c) => UnitFormat.Temperature(c, app!.S.Temperature);

    protected override void OnPaint(PaintEventArgs e)
    {
        if (app?.Forecast is not { } f) return;
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        var t = new Theme(dark);
        var localNow = app.LocalNow;
        int pad = Px(12);
        using var fName = new Font("Segoe UI Semibold", 9.5f);
        using var fSmall = new Font("Segoe UI", 8.5f);
        using var fTime = new Font("Segoe UI", 8f);
        using var fTemp = new Font("Segoe UI Semibold", 9.5f);

        // Top line: the place, and rain about to start or stop (or the condition) on the far side.
        int farW = Px(150);
        TextRenderer.DrawText(g, app.ActivePlace?.Name ?? "", fName, M(new Rectangle(pad, Px(8), Width - pad * 2 - farW - Px(6), Px(20))), t.Text,
            Near | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
        bool fresh = (DateTime.UtcNow - f.FetchedUtc).TotalMinutes < 75;
        var change = fresh ? RainAlert.Check(f.Minutely, localNow) : null;
        string far = change is { } c ? L.F(c.Starting ? "RainAround" : "DryAround", L.Clock(c.At))
                   : app.Now() is { } now ? L.Condition(now.code) : "";
        TextRenderer.DrawText(g, far, fSmall, M(new Rectangle(Width - pad - farW, Px(8), farW, Px(20))), change != null ? t.Rain : t.Dim,
            Far | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);

        // The next six hours: time, picture, temperature and rain chance.
        var hours = f.HoursFrom(localNow, Hours);
        int top = Px(34), colW = (Width - pad * 2) / Hours;
        for (int i = 0; i < hours.Count; i++)
        {
            int col = Rtl ? Hours - 1 - i : i;
            var cr = new Rectangle(pad + col * colW, top, colW, Px(80));
            string label = i == 0 ? L.T("Now") : L.Hour(hours[i].Time);
            TextRenderer.DrawText(g, label, fTime, new Rectangle(cr.X, cr.Y, cr.Width, Px(16)), t.Dim, TextFormatFlags.HorizontalCenter);
            int s = Px(24);
            Art.Weather(g, new RectangleF(cr.X + (cr.Width - s) / 2f, cr.Y + Px(17), s, s), WeatherCode.Icon(hours[i].Code), hours[i].IsDay);
            TextRenderer.DrawText(g, Num(hours[i].TemperatureC), fTemp, new Rectangle(cr.X, cr.Y + Px(43), cr.Width, Px(18)), t.Text, TextFormatFlags.HorizontalCenter);
            if (hours[i].PrecipitationChance >= 10)
                TextRenderer.DrawText(g, UnitFormat.Percent(hours[i].PrecipitationChance), fTime, new Rectangle(cr.X, cr.Y + Px(61), cr.Width, Px(16)), t.Rain, TextFormatFlags.HorizontalCenter);
        }

        // Bottom line: wind on the near side, air quality on the far side.
        int y = Px(H - 32);
        using (var pen = new Pen(t.Border, Math.Max(1, S))) g.DrawLine(pen, pad, y, Width - pad, y);
        // Numbers with units stay left to right inside Arabic text.
        string wind = $"{L.T("Wind")} {L.Ltr(UnitFormat.Wind(f.Current.WindKmh, app.S.Wind))} {L.T($"Compass{UnitConvert.Compass(f.Current.WindDirection)}")}";
        TextRenderer.DrawText(g, wind, fSmall, M(new Rectangle(pad, y + Px(4), Width / 2, Px(24))), t.Dim, Near | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
        if (app.S.ShowAirQuality && app.Air is { EuropeanAqi: { } aqi, Category: { } cat })
        {
            int dotS = Px(8);
            string airText = $"{L.T("Air")} {L.Ltr(aqi.ToString(System.Globalization.CultureInfo.InvariantCulture))} {L.T("Air" + cat)}";
            int tw = TextRenderer.MeasureText(g, airText, fSmall, Size.Empty, TextFormatFlags.NoPadding).Width;
            var tr = M(new Rectangle(Width - pad - tw, y + Px(4), tw, Px(24)));
            TextRenderer.DrawText(g, airText, fSmall, tr, t.Dim, Far | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
            var dot = M(new RectangleF(Width - pad - tw - Px(6) - dotS, y + Px(4) + (Px(24) - dotS) / 2f, dotS, dotS));
            using var b = new SolidBrush(Art.AirColor(cat));
            g.FillEllipse(b, dot);
        }
    }
}
