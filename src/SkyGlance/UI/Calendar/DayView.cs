using System.Drawing.Drawing2D;
using System.Globalization;

namespace SkyGlance.UI.Calendar;

/// <summary>The weather of the picked day: a header like the flyout's, six tiles and the day in eight steps of three hours.</summary>
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

        // The day in eight steps: 00:00, 03:00 ... 21:00.
        var card = new Rectangle(0, y, Width, Math.Max(Px(90), Height - y - 1));
        Art.FillRound(g, card, Px(10), t.Surface);
        using var fTime = new Font("Segoe UI", 8f);
        using var fTemp = new Font("Segoe UI Semibold", 9.5f);
        var steps = r.Hours.Where(h => h.Time.Hour % 3 == 0).Take(8).ToList();
        if (steps.Count == 0) return;
        int colW = card.Width / steps.Count;
        for (int i = 0; i < steps.Count; i++)
        {
            int col = Rtl ? steps.Count - 1 - i : i;
            var cr = new Rectangle(card.X + col * colW, card.Y, colW, card.Height);
            TextRenderer.DrawText(g, L.Hour(steps[i].Time), fTime, new Rectangle(cr.X, cr.Y + Px(10), cr.Width, Px(16)), t.Dim, TextFormatFlags.HorizontalCenter);
            int s = Px(26);
            Art.Weather(g, new RectangleF(cr.X + (cr.Width - s) / 2f, cr.Y + Px(30), s, s), WeatherCode.Icon(steps[i].Code), steps[i].IsDay);
            TextRenderer.DrawText(g, Num(steps[i].TemperatureC), fTemp, new Rectangle(cr.X, cr.Y + Px(60), cr.Width, Px(20)), t.Text, TextFormatFlags.HorizontalCenter);
        }
    }
}
