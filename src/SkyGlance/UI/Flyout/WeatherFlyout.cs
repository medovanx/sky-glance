using System.Drawing.Drawing2D;

namespace SkyGlance.UI.Flyout;

/// <summary>
/// The popup above the taskbar: the sky right now in a gradient header, six detail tiles,
/// a 24-hour temperature chart and the days ahead, with shortcuts to refresh, location and settings.
/// Everything is laid out left to right and mirrored for Arabic.
/// </summary>
sealed class WeatherFlyout : Form
{
    const int W = 380, HeaderH = 136, TileH = 46, HourlyH = 128, DayH = 26, Pad = 16, Gap = 12, ButtonH = 34;

    readonly SkyGlanceApp app;
    readonly IconButton btnRefresh = new(), btnLocation = new(), btnSettings = new();
    bool dark, activated;
    int dayRows = 7;
    public DateTime LastHidden { get; private set; }

    public WeatherFlyout(SkyGlanceApp app)
    {
        this.app = app;
        FormBorderStyle = FormBorderStyle.None;
        ShowInTaskbar = false;
        TopMost = true;
        StartPosition = FormStartPosition.Manual;
        DoubleBuffered = true;
        KeyPreview = true;
        Text = L.T("AppName");
        Icon = app.AppIcon;
        btnRefresh.Glyph = Art.IconRefresh;
        btnLocation.Glyph = Art.IconLocation;
        btnSettings.Glyph = Art.IconSettings;
        Controls.AddRange([btnRefresh, btnLocation, btnSettings]);
        btnRefresh.Click += async (_, _) => await RefreshNowAsync();
        btnLocation.Click += (_, _) => { Hide(); app.ShowSettings(SettingsForm.LocationPage); };
        btnSettings.Click += (_, _) => { Hide(); app.ShowSettings(); };
        KeyDown += (_, e) => { if (e.KeyCode == Keys.Escape) Hide(); else if (e.KeyCode == Keys.F5) _ = RefreshNowAsync(); };
        app.Changed += () => { if (Visible && !IsDisposed) { Relayout(); Invalidate(); } };
    }

    async Task RefreshNowAsync()
    {
        if (!btnRefresh.Enabled) return;
        btnRefresh.Enabled = false;
        await app.RefreshAsync();
        btnRefresh.Enabled = true;
    }

    protected override CreateParams CreateParams
    {
        get
        {
            var cp = base.CreateParams;
            cp.ClassStyle |= 0x20000;   // CS_DROPSHADOW
            cp.ExStyle |= 0x80;         // WS_EX_TOOLWINDOW
            return cp;
        }
    }

    protected override bool ShowWithoutActivation => app.Headless;

    float S => DeviceDpi / 96f;
    int Px(float v) => (int)Math.Round(v * S);
    bool Rtl => L.Ar;

    /// <summary>Mirrors a rectangle laid out left to right when the UI is Arabic.</summary>
    Rectangle M(Rectangle r) => Rtl ? new Rectangle(Width - r.Right, r.Y, r.Width, r.Height) : r;
    RectangleF M(RectangleF r) => Rtl ? new RectangleF(Width - r.Right, r.Y, r.Width, r.Height) : r;
    float Mx(float x) => Rtl ? Width - x : x;

    bool HasData => app.Forecast != null;

    int BodyHeight(int days) => HasData
        ? Gap + TileH * 2 + 6 + Gap + HourlyH + Gap + 26 + days * DayH + 8 + 18 + 8
        : 90;

    int ContentHeight(int days) => HeaderH + BodyHeight(days) + ButtonH + Pad;

    void Prepare(bool darkTheme)
    {
        dark = darkTheme;
        var theme = new Theme(dark);
        RightToLeft = Rtl ? RightToLeft.Yes : RightToLeft.No;
        BackColor = theme.Window;
        btnRefresh.Text = L.T("Refresh");
        btnLocation.Text = L.T("Location");
        btnSettings.Text = L.T("Settings");
        foreach (var b in new[] { btnRefresh, btnLocation, btnSettings }) { b.Theme = theme; b.RightToLeft = RightToLeft; b.Invalidate(); }
        btnSettings.Primary = true;
    }

    public void ShowNear(Rectangle anchor)
    {
        Prepare(Native.SystemDark());
        var wa = Screen.FromRectangle(anchor).WorkingArea;
        Width = Px(W);
        FitDays(wa.Height - Px(20));
        int x = anchor.Left + anchor.Width / 2 - Width / 2;
        x = Math.Clamp(x, wa.Left + Px(8), wa.Right - Width - Px(8));
        Location = new Point(x, wa.Bottom - Height - Px(10));
        LayoutButtons();

        activated = false;
        Show();
        Native.RoundCorners(Handle, dark);
        Activate();
        Native.SetForegroundWindow(Handle);
    }

    /// <summary>Lays the flyout out off-screen for --shots, without showing or focusing it.</summary>
    public void ShowForShot(bool darkTheme)
    {
        Prepare(darkTheme);
        Location = new Point(-4000, -4000);
        Width = Px(W);
        FitDays(int.MaxValue);
        LayoutButtons();
        Show();
        Location = new Point(-4000, -4000);
    }

    /// <summary>Shows as many days as fit on the screen (at least three), so the flyout never runs off it at high scaling.</summary>
    void FitDays(int maxHeight)
    {
        int available = app.Forecast?.DaysFrom(app.LocalNow).Count ?? 0;
        dayRows = Math.Min(7, available);
        while (dayRows > 3 && Px(ContentHeight(dayRows)) > maxHeight) dayRows--;
        Height = Px(ContentHeight(dayRows));
    }

    void Relayout()
    {
        int bottom = Bottom;
        var wa = Screen.FromControl(this).WorkingArea;
        FitDays(wa.Height - Px(20));
        Top = bottom - Height;
        LayoutButtons();
    }

    void LayoutButtons()
    {
        int pad = Px(Pad), gap = Px(8), bh = Px(ButtonH);
        int bw = (Width - pad * 2 - gap * 2) / 3, by = Height - pad - bh;
        var order = Rtl ? new[] { btnSettings, btnLocation, btnRefresh } : [btnRefresh, btnLocation, btnSettings];
        for (int i = 0; i < 3; i++) order[i].Bounds = new Rectangle(pad + i * (bw + gap), by, bw, bh);
    }

    protected override void OnActivated(EventArgs e)
    {
        base.OnActivated(e);
        activated = true;
    }

    protected override void OnDeactivate(EventArgs e)
    {
        base.OnDeactivate(e);
        if (activated) Hide();
    }

    protected override void OnVisibleChanged(EventArgs e)
    {
        base.OnVisibleChanged(e);
        if (!Visible) LastHidden = DateTime.Now;
    }

    // ---------- painting ----------

    TextFormatFlags Near => Rtl ? TextFormatFlags.Right | TextFormatFlags.RightToLeft : TextFormatFlags.Left;
    TextFormatFlags Far => Rtl ? TextFormatFlags.Left | TextFormatFlags.RightToLeft : TextFormatFlags.Right;

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        var t = new Theme(dark);
        var f = app.Forecast;
        var now = app.Now();
        var localNow = app.LocalNow;

        DrawHeader(g, f, now, localNow);
        int y = Px(HeaderH);

        if (f == null || now == null)
        {
            string msg = !app.S.HasLocation && !app.Fetching ? L.T("NoLocation")
                       : app.LastFetchFailed ? L.T("OfflineNoData") : L.T("Loading");
            using var fMsg = new Font("Segoe UI", 10f);
            TextRenderer.DrawText(g, msg, fMsg, new Rectangle(Px(Pad), y, Width - Px(Pad) * 2, Px(90)), t.Dim,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.WordBreak | (Rtl ? TextFormatFlags.RightToLeft : 0));
            return;
        }

        y += Px(Gap);
        y = DrawTiles(g, t, f, localNow, y);
        y += Px(Gap);
        DrawHourly(g, t, f, localNow, new Rectangle(Px(Pad), y, Width - Px(Pad) * 2, Px(HourlyH)));
        y += Px(HourlyH) + Px(Gap);
        y = DrawDays(g, t, f, localNow, y);

        using var fNote = new Font("Segoe UI", 8f);
        TextRenderer.DrawText(g, L.T("DataBy"), fNote, new Rectangle(Px(Pad), y + Px(8), Width - Px(Pad) * 2, Px(18)), t.Dim,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
    }

    void DrawHeader(Graphics g, Forecast? f, (double tempC, int code, bool day)? now, DateTime localNow)
    {
        var head = new Rectangle(0, 0, Width, Px(HeaderH));
        var icon = now is { } n ? WeatherCode.Icon(n.code) : SkyIcon.Cloudy;
        bool isDay = now?.day ?? true;
        var (c1, c2) = Art.SkyColors(icon, isDay);
        using (var grad = new LinearGradientBrush(head, c1, c2, LinearGradientMode.ForwardDiagonal))
            g.FillRectangle(grad, head);
        Art.CloudPattern(g, head, Px(46), Color.FromArgb(18, 255, 255, 255));

        int pad = Px(Pad);
        using var fName = new Font("Segoe UI Semibold", 10.5f);
        using var fSmall = new Font("Segoe UI", 8.5f);
        using var fBig = new Font("Segoe UI Semibold", 30f);
        using var fCond = new Font("Segoe UI Semibold", 11f);
        var soft = Color.FromArgb(220, 232, 244);

        // Place name, and the status on the far side.
        string place = app.S.HasLocation ? app.S.LocationName : L.T("NoLocation");
        string status = StatusText(f);
        int statusW = Px(150);
        TextRenderer.DrawText(g, place, fName, M(new Rectangle(pad, Px(10), Width - pad * 2 - statusW, Px(24))), Color.White,
            Near | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
        TextRenderer.DrawText(g, status, fSmall, M(new Rectangle(Width - pad - statusW, Px(10), statusW, Px(24))), soft,
            Far | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);

        if (now is not { } cur || f == null) return;

        int iconS = Px(68);
        Art.Weather(g, M(new RectangleF(pad - Px(4), Px(40), iconS, iconS)), icon, isDay);
        string temp = Num(cur.tempC);
        var tempR = M(new Rectangle(pad + iconS + Px(4), Px(34), Px(130), Px(56)));
        TextRenderer.DrawText(g, temp, fBig, tempR, Color.White, (Rtl ? TextFormatFlags.Right : TextFormatFlags.Left) | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);

        var today = f.Today(localNow);
        int farW = Px(150);
        var farX = Width - pad - farW;
        TextRenderer.DrawText(g, L.Condition(cur.code), fCond, M(new Rectangle(farX, Px(44), farW, Px(22))), Color.White,
            Far | TextFormatFlags.EndEllipsis);
        if (today != null)
            Run(g, [L.T("High"), Num(today.MaxC), "  " + L.T("Low"), Num(today.MinC)], fSmall, soft, new Rectangle(farX, Px(68), farW, Px(18)), far: true);
        bool fresh = (DateTime.UtcNow - f.FetchedUtc).TotalMinutes < 75;
        if (fresh)
            Run(g, [L.T("FeelsLike"), Num(f.Current.FeelsLikeC)], fSmall, soft, new Rectangle(farX, Px(86), farW, Px(18)), far: true);

        // Date at the place, plus its clock when it is in another time zone than this PC.
        string date = L.LongDay(localNow);
        if (!app.Headless && Math.Abs((localNow - DateTime.Now).TotalMinutes) > 1) date += "   " + L.F("LocalTime", L.Clock(localNow));
        TextRenderer.DrawText(g, date, fSmall, M(new Rectangle(pad, Px(110), Width - pad * 2, Px(18))), soft, Near | TextFormatFlags.EndEllipsis);
    }

    /// <summary>A temperature with Latin digits, drawn on its own so no bidi rule can move its sign or degree mark.</summary>
    string Num(double c) => UnitFormat.Temperature(c, app.S.Temperature);

    /// <summary>
    /// Draws pieces of mixed text (Arabic words, numbers with units) one by one, in reading order: left to right in
    /// English, right to left in Arabic. Each piece is drawn alone, so "-3°" or "16 km/h" can never be reordered.
    /// <paramref name="r"/> is in left-to-right coordinates unless <paramref name="mirrored"/> says it already is final.
    /// </summary>
    void Run(Graphics g, string[] parts, Font font, Color color, Rectangle r, bool far, bool mirrored = false)
    {
        if (!mirrored) r = M(r);
        const TextFormatFlags flags = TextFormatFlags.NoPadding | TextFormatFlags.SingleLine | TextFormatFlags.VerticalCenter;
        int space = TextRenderer.MeasureText(g, " ", font, Size.Empty, flags).Width + Px(1);
        var widths = parts.Select(p => TextRenderer.MeasureText(g, p.TrimStart(), font, Size.Empty, flags).Width + (p.StartsWith("  ") ? space * 2 : 0)).ToArray();
        int total = widths.Sum() + space * (parts.Length - 1);
        // Where the run starts on screen: aligned to the near or far side of the reading direction.
        bool alignRight = Rtl ? !far : far;
        int x = alignRight ? r.Right - total : r.Left;
        var order = Enumerable.Range(0, parts.Length);
        if (Rtl) order = order.Reverse();
        foreach (int i in order)
        {
            string p = parts[i];
            int lead = p.StartsWith("  ") ? space * 2 : 0;
            bool leadOnLeft = !Rtl;
            int textX = x + (leadOnLeft ? lead : 0);
            TextRenderer.DrawText(g, p.TrimStart(), font, new Rectangle(textX, r.Y, widths[i] - lead + Px(2), r.Height), color, flags | TextFormatFlags.Left);
            x += widths[i] + space;
        }
    }

    string StatusText(Forecast? f)
    {
        if (app.Fetching) return L.T("Loading");
        if (f == null) return "";
        if (app.LastFetchFailed) return L.T("Offline");
        int mins = (int)(DateTime.UtcNow - f.FetchedUtc).TotalMinutes;
        if (mins < 1) return L.T("UpdatedNow");
        if (mins < 120) return L.F("UpdatedAgo", mins);
        return L.F("UpdatedAt", L.Clock(f.FetchedUtc.ToLocalTime()));
    }

    int DrawTiles(Graphics g, Theme t, Forecast f, DateTime localNow, int y)
    {
        var today = f.Today(localNow);
        var c = f.Current;
        string Sun(DateTime? v) => v is { } x ? L.Clock(x) : L.T("NoSun");
        string compass = L.T($"Compass{UnitConvert.Compass(c.WindDirection)}");
        int chance = f.HoursFrom(localNow, 1).FirstOrDefault()?.PrecipitationChance ?? today?.PrecipitationChance ?? 0;
        var tiles = new (string label, string[] value)[]
        {
            (L.T("Humidity"), [UnitFormat.Percent(c.Humidity)]),
            (L.T("Wind"), [UnitFormat.Wind(c.WindKmh, app.S.Wind), compass]),
            (L.T("UvIndex"), [today != null ? Math.Round(today.UvIndexMax).ToString(System.Globalization.CultureInfo.InvariantCulture) : "-"]),
            (L.T("RainChance"), c.PrecipitationMm > 0
                ? [UnitFormat.Percent(chance), UnitFormat.Precipitation(c.PrecipitationMm, app.S.Precipitation)]
                : [UnitFormat.Percent(chance)]),
            (L.T("Sunrise"), [Sun(today?.Sunrise)]),
            (L.T("Sunset"), [Sun(today?.Sunset)]),
        };
        using var fLabel = new Font("Segoe UI", 8f);
        using var fValue = new Font("Segoe UI Semibold", 10f);
        int pad = Px(Pad), gap = Px(6), tw = (Width - pad * 2 - gap * 2) / 3, th = Px(TileH);
        for (int i = 0; i < tiles.Length; i++)
        {
            int col = i % 3, row = i / 3;
            var r = M(new Rectangle(pad + col * (tw + gap), y + row * (th + gap), tw, th));
            Art.FillRound(g, r, Px(8), t.Surface);
            var inner = Rectangle.Inflate(r, -Px(10), 0);
            TextRenderer.DrawText(g, tiles[i].label, fLabel, new Rectangle(inner.X, r.Y + Px(5), inner.Width, Px(16)), t.Dim, Near | TextFormatFlags.EndEllipsis);
            Run(g, tiles[i].value, fValue, t.Text, new Rectangle(inner.X, r.Y + Px(21), inner.Width, Px(20)), far: false, mirrored: true);
        }
        return y + th * 2 + gap;
    }

    void DrawHourly(Graphics g, Theme t, Forecast f, DateTime localNow, Rectangle card)
    {
        Art.FillRound(g, card, Px(10), t.Surface);
        using var fTitle = new Font("Segoe UI Semibold", 9.5f);
        using var fSmall = new Font("Segoe UI", 8f);
        using var fTemp = new Font("Segoe UI Semibold", 8.5f);
        TextRenderer.DrawText(g, L.T("Next24Hours"), fTitle, M(new Rectangle(card.X + Px(12), card.Y + Px(6), card.Width - Px(24), Px(20))), t.Text, Near);

        var hours = f.HoursFrom(localNow, 25);
        if (hours.Count < 2) return;
        // Chart band: temperature curve with a value every 3 hours, rain chance as faint bars, icon and time below.
        var plot = new RectangleF(card.X + Px(22), card.Y + Px(44), card.Width - Px(44), Px(28));
        double lo = hours.Min(h => h.TemperatureC), hi = hours.Max(h => h.TemperatureC);
        if (hi - lo < 2) { lo -= 1; hi += 1; }
        PointF P(int i) => new(Mx(plot.Left + plot.Width * i / (hours.Count - 1)), plot.Bottom - (float)((hours[i].TemperatureC - lo) / (hi - lo)) * plot.Height);

        float barBottom = card.Bottom - Px(40);
        using (var rain = new SolidBrush(Color.FromArgb(t.Dark ? 60 : 45, t.Rain)))
            for (int i = 0; i < hours.Count; i++)
            {
                if (hours[i].PrecipitationChance <= 0) continue;
                float bh = (barBottom - plot.Top) * hours[i].PrecipitationChance / 100f;
                float bw = Math.Max(2f, plot.Width / hours.Count - Px(2));
                g.FillRectangle(rain, P(i).X - bw / 2, barBottom - bh, bw, bh);
            }

        var pts = Enumerable.Range(0, hours.Count).Select(P).ToArray();
        using (var area = new GraphicsPath())
        {
            area.AddCurve(pts, 0.4f);
            area.AddLine(pts[^1], new PointF(pts[^1].X, barBottom));
            area.AddLine(new PointF(pts[^1].X, barBottom), new PointF(pts[0].X, barBottom));
            area.CloseFigure();
            using var fill = new LinearGradientBrush(new RectangleF(plot.Left, plot.Top - Px(4), plot.Width, barBottom - plot.Top + Px(8)),
                Color.FromArgb(90, t.Warm), Color.FromArgb(0, t.Warm), LinearGradientMode.Vertical);
            g.FillPath(fill, area);
        }
        using (var pen = new Pen(t.Warm, 1.8f * S)) g.DrawCurve(pen, pts, 0.4f);

        for (int i = 0; i < hours.Count; i += 3)
        {
            var p = pts[i];
            using (var dot = new SolidBrush(t.Warm)) g.FillEllipse(dot, p.X - Px(3), p.Y - Px(3), Px(6), Px(6));
            var tr = new Rectangle((int)p.X - Px(20), (int)p.Y - Px(20), Px(40), Px(16));
            TextRenderer.DrawText(g, Num(hours[i].TemperatureC), fTemp, tr, t.Text, TextFormatFlags.HorizontalCenter);
            int iconS = Px(18);
            Art.Weather(g, new RectangleF(p.X - iconS / 2f, card.Bottom - Px(38), iconS, iconS), WeatherCode.Icon(hours[i].Code), hours[i].IsDay);
            string label = i == 0 ? L.T("Now") : L.Hour(hours[i].Time);
            TextRenderer.DrawText(g, label, fSmall, new Rectangle((int)p.X - Px(24), card.Bottom - Px(19), Px(48), Px(16)), t.Dim, TextFormatFlags.HorizontalCenter);
        }
    }

    int DrawDays(Graphics g, Theme t, Forecast f, DateTime localNow, int y)
    {
        using var fTitle = new Font("Segoe UI Semibold", 9.5f);
        using var fDay = new Font("Segoe UI", 9f);
        using var fNum = new Font("Segoe UI Semibold", 9f);
        using var fSmall = new Font("Segoe UI", 8f);
        int pad = Px(Pad);
        TextRenderer.DrawText(g, L.T("SevenDays"), fTitle, M(new Rectangle(pad + Px(4), y, Width - pad * 2, Px(22))), t.Text, Near);
        y += Px(26);
        var days = f.DaysFrom(localNow).Take(dayRows).ToList();
        if (days.Count == 0) return y;
        double lo = days.Min(d => d.MinC), hi = days.Max(d => d.MaxC);
        if (hi - lo < 1) hi = lo + 1;
        var today = DateOnly.FromDateTime(localNow);

        int dayW = Px(88), iconW = Px(26), rainW = Px(42), numW = Px(38);
        int barL = pad + Px(6) + dayW + iconW + rainW + numW, barR = Width - pad - Px(6) - numW;
        foreach (var d in days)
        {
            var row = new Rectangle(pad, y, Width - pad * 2, Px(DayH) - Px(2));
            if (d.Date == today) Art.FillRound(g, row, Px(6), Color.FromArgb(t.Dark ? 30 : 20, t.Accent));
            int x = pad + Px(6);
            string name = d.Date == today ? L.T("Today") : L.DayName(d.Date);
            TextRenderer.DrawText(g, name, fDay, M(new Rectangle(x, y, dayW, row.Height)), t.Text, Near | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
            x += dayW;
            Art.Weather(g, M(new RectangleF(x, y + (row.Height - Px(20)) / 2f, Px(20), Px(20))), WeatherCode.Icon(d.Code), true);
            x += iconW;
            if (d.PrecipitationChance >= 10)
                TextRenderer.DrawText(g, UnitFormat.Percent(d.PrecipitationChance), fSmall, M(new Rectangle(x, y, rainW, row.Height)), t.Rain,
                    (Rtl ? TextFormatFlags.Right : TextFormatFlags.Left) | TextFormatFlags.VerticalCenter);
            x += rainW;
            TextRenderer.DrawText(g, Num(d.MinC), fNum, M(new Rectangle(x, y, numW - Px(6), row.Height)), t.Dim,
                (Rtl ? TextFormatFlags.Left : TextFormatFlags.Right) | TextFormatFlags.VerticalCenter);

            // Range bar: the whole week's span in a faint track, this day's span in a cool-to-warm gradient.
            var track = M(new RectangleF(barL, y + row.Height / 2f - Px(2), barR - barL, Px(4)));
            Art.FillRound(g, track, Px(2), Color.FromArgb(t.Dark ? 50 : 35, t.Text));
            float a = (float)((d.MinC - lo) / (hi - lo)), b = (float)((d.MaxC - lo) / (hi - lo));
            var span = M(new RectangleF(barL + (barR - barL) * a, track.Y, Math.Max(Px(4), (barR - barL) * (b - a)), Px(4)));
            using (var p = Art.RoundRect(span, Px(2)))
            using (var br = new LinearGradientBrush(new RectangleF(span.X - 1, span.Y, span.Width + 2, span.Height), Rtl ? t.Warm : t.Cool, Rtl ? t.Cool : t.Warm, LinearGradientMode.Horizontal))
                g.FillPath(br, p);

            TextRenderer.DrawText(g, Num(d.MaxC), fNum, M(new Rectangle(barR + Px(6), y, numW, row.Height)), t.Text,
                (Rtl ? TextFormatFlags.Right : TextFormatFlags.Left) | TextFormatFlags.VerticalCenter);
            y += Px(DayH);
        }
        return y;
    }
}
