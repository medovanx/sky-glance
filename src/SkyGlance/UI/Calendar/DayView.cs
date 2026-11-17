using System.Drawing.Drawing2D;
using System.Globalization;

namespace SkyGlance.UI.Calendar;

/// <summary>The weather of the picked day: a header like the flyout's, six tiles and the day hour by hour.</summary>
sealed class DayView : Control
{
    readonly SkyGlanceApp app;
    public Theme Theme { get; set; } = Theme.Current;
    public DayReport? Report { get; private set; }
    public string PlaceName { get; set; } = "";
    public DateOnly Date { get; set; }
    /// <summary>A message instead of a report: loading, out of range or failed.</summary>
    public string? Message { get; private set; }

    public DayView(SkyGlanceApp app)
    {
        this.app = app;
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
    }

    public void Show(DayReport r) { Report = r; Message = null; Invalidate(); }
    public void ShowMessage(string m) { Report = null; Message = m; Invalidate(); }

    float S => DeviceDpi / 96f;
    int Px(float v) => (int)Math.Round(v * S);
    bool Rtl => RightToLeft == RightToLeft.Yes;
    Rectangle M(Rectangle r) => Rtl ? new Rectangle(Width - r.Right, r.Y, r.Width, r.Height) : r;
    RectangleF M(RectangleF r) => Rtl ? new RectangleF(Width - r.Right, r.Y, r.Width, r.Height) : r;
    TextFormatFlags Near => Rtl ? TextFormatFlags.Right | TextFormatFlags.RightToLeft : TextFormatFlags.Left;
    TextFormatFlags Far => Rtl ? TextFormatFlags.Left | TextFormatFlags.RightToLeft : TextFormatFlags.Right;
    string Num(double c) => UnitFormat.Temperature(c, app.S.Temperature);

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        var t = Theme;
        g.Clear(Styler.Backdrop(this, t.Window));
        var r = Report;

        // Header card: sky gradient for the day's weather, the date, the place and where the data comes from.
        var head = new Rectangle(0, 0, Width, Px(150));
        var icon = r != null ? WeatherCode.Icon(r.Code) : SkyIcon.Cloudy;
        var (c1, c2) = Art.SkyColors(icon, true);
        using (var p = Art.RoundRect(new RectangleF(0, 0, Width - 1, head.Height), Px(10)))
        {
            using var grad = new LinearGradientBrush(head, c1, c2, LinearGradientMode.ForwardDiagonal);
            g.FillPath(grad, p);
            var st = g.Save();
            g.SetClip(p);
            Art.CloudPattern(g, head, Px(46), Color.FromArgb(18, 255, 255, 255));
            g.Restore(st);
        }
        using var fDate = new Font("Segoe UI Semibold", 12f);
        using var fSmall = new Font("Segoe UI", 8.5f);
        using var fBig = new Font("Segoe UI Semibold", 26f);
        using var fCond = new Font("Segoe UI Semibold", 11f);
        var soft = Color.FromArgb(220, 232, 244);
        int pad = Px(16);
        string date = Date.ToDateTime(TimeOnly.MinValue).ToString(L.Ar ? "dddd d MMMM yyyy" : "dddd d MMMM yyyy", L.Culture);
        TextRenderer.DrawText(g, date, fDate, M(new Rectangle(pad, Px(10), Width - pad * 2, Px(26))), Color.White, Near | TextFormatFlags.EndEllipsis);
        TextRenderer.DrawText(g, PlaceName, fSmall, M(new Rectangle(pad, Px(36), Width - pad * 2, Px(18))), soft, Near | TextFormatFlags.EndEllipsis);

        if (r == null)
        {
            using var fMsg = new Font("Segoe UI", 10f);
            TextRenderer.DrawText(g, Message ?? "", fMsg, new Rectangle(pad, head.Bottom + Px(20), Width - pad * 2, Px(80)), t.Dim,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.WordBreak | (Rtl ? TextFormatFlags.RightToLeft : 0));
            return;
        }

        int iconS = Px(64);
        Art.Weather(g, M(new RectangleF(pad - Px(4), Px(64), iconS, iconS)), icon, true);
        TextRenderer.DrawText(g, Num(r.MaxC), fBig, M(new Rectangle(pad + iconS + Px(6), Px(62), Px(110), Px(48))), Color.White,
            (Rtl ? TextFormatFlags.Right : TextFormatFlags.Left) | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
        TextRenderer.DrawText(g, Num(r.MinC), fCond, M(new Rectangle(pad + iconS + Px(8), Px(108), Px(100), Px(22))), soft,
            (Rtl ? TextFormatFlags.Right : TextFormatFlags.Left) | TextFormatFlags.NoPadding);
        int farW = Px(200), farX = Width - pad - farW;
        TextRenderer.DrawText(g, L.Condition(r.Code), fCond, M(new Rectangle(farX, Px(70), farW, Px(22))), Color.White, Far | TextFormatFlags.EndEllipsis);
        string source = r.Source == DaySource.Archive ? L.T("SourceArchive") : Date > DateOnly.FromDateTime(app.LocalNow) ? L.T("SourceForecast") : L.T("SourceRecent");
        TextRenderer.DrawText(g, source, fSmall, M(new Rectangle(farX, Px(96), farW, Px(18))), soft, Far | TextFormatFlags.EndEllipsis);

        // Tiles.
        string Sun(DateTime? v) => v is { } x ? L.Clock(x) : L.T("NoSun");
        var tiles = new (string label, string value)[]
        {
            (L.T("Precipitation"), UnitFormat.Precipitation(r.PrecipitationMm, app.S.Precipitation)),
            (L.T("RainChance"), r.PrecipitationChance is { } ch ? UnitFormat.Percent(ch) : "-"),
            (L.T("WindMax"), UnitFormat.Wind(r.WindMaxKmh, app.S.Wind)),
            (L.T("UvIndex"), r.UvIndexMax is { } uv ? Math.Round(uv).ToString(CultureInfo.InvariantCulture) : "-"),
            (L.T("Sunrise"), Sun(r.Sunrise)),
            (L.T("Sunset"), Sun(r.Sunset)),
        };
        using var fLabel = new Font("Segoe UI", 8f);
        using var fValue = new Font("Segoe UI Semibold", 10f);
        int gap = Px(6), tw = (Width - gap * 2) / 3, th = Px(46), y = head.Bottom + Px(12);
        for (int i = 0; i < tiles.Length; i++)
        {
            var tr = M(new Rectangle((i % 3) * (tw + gap), y + (i / 3) * (th + gap), tw, th));
            Art.FillRound(g, tr, Px(8), t.Surface);
            var inner = Rectangle.Inflate(tr, -Px(10), 0);
            TextRenderer.DrawText(g, tiles[i].label, fLabel, new Rectangle(inner.X, tr.Y + Px(5), inner.Width, Px(16)), t.Dim, Near | TextFormatFlags.EndEllipsis);
            TextRenderer.DrawText(g, tiles[i].value, fValue, new Rectangle(inner.X, tr.Y + Px(21), inner.Width, Px(20)), t.Text,
                (Rtl ? TextFormatFlags.Right : TextFormatFlags.Left) | TextFormatFlags.EndEllipsis);
        }
        y += th * 2 + gap + Px(12);

        // The day hour by hour: the temperature as a curve with a value every three hours, rain as bars
        // (the chance for a forecast day, the amount that fell for a past one), the picture and time below.
        var card = new Rectangle(0, y, Width, Math.Max(Px(120), Height - y - 1));
        Art.FillRound(g, card, Px(10), t.Surface);
        using var fTitle = new Font("Segoe UI Semibold", 9.5f);
        using var fTime = new Font("Segoe UI", 8f);
        using var fTemp = new Font("Segoe UI Semibold", 8.5f);
        TextRenderer.DrawText(g, L.T("HourByHour"), fTitle, M(new Rectangle(card.X + Px(12), card.Y + Px(6), card.Width - Px(24), Px(20))), t.Text, Near);
        var hours = r.Hours.OrderBy(h => h.Time).Take(24).ToList();
        if (hours.Count < 2) return;
        var plot = new RectangleF(card.X + Px(22), card.Y + Px(46), card.Width - Px(44), Px(26));
        double lo = hours.Min(h => h.TemperatureC), hi = hours.Max(h => h.TemperatureC);
        if (hi - lo < 2) { lo -= 1; hi += 1; }
        float X(int i) { float x = plot.Left + plot.Width * i / (hours.Count - 1); return Rtl ? Width - x : x; }
        PointF P(int i) => new(X(i), plot.Bottom - (float)((hours[i].TemperatureC - lo) / (hi - lo)) * plot.Height);

        float barBottom = card.Bottom - Px(40);
        bool chances = hours.Any(h => h.PrecipitationChance > 0);
        double maxMm = Math.Max(1, hours.Max(h => h.PrecipitationMm));
        using (var rain = new SolidBrush(Color.FromArgb(t.Dark ? 60 : 45, t.Rain)))
            for (int i = 0; i < hours.Count; i++)
            {
                float share = chances ? hours[i].PrecipitationChance / 100f : (float)(hours[i].PrecipitationMm / maxMm);
                if (share <= 0) continue;
                float bh = (barBottom - plot.Top) * share, bw = Math.Max(2f, plot.Width / hours.Count - Px(2));
                g.FillRectangle(rain, X(i) - bw / 2, barBottom - bh, bw, bh);
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
            TextRenderer.DrawText(g, Num(hours[i].TemperatureC), fTemp, new Rectangle((int)p.X - Px(20), (int)p.Y - Px(20), Px(40), Px(16)), t.Text, TextFormatFlags.HorizontalCenter);
            int hourIcon = Px(18);
            Art.Weather(g, new RectangleF(p.X - hourIcon / 2f, card.Bottom - Px(38), hourIcon, hourIcon), WeatherCode.Icon(hours[i].Code), hours[i].IsDay);
            TextRenderer.DrawText(g, L.Hour(hours[i].Time), fTime, new Rectangle((int)p.X - Px(24), card.Bottom - Px(19), Px(48), Px(16)), t.Dim, TextFormatFlags.HorizontalCenter);
        }
    }
}
