using System.Drawing.Drawing2D;
using System.Globalization;

namespace SkyGlance.UI.Controls;

/// <summary>
/// A month grid in the app's style: arrows and the mouse wheel change month, the keyboard moves the day,
/// and days outside <see cref="MinDate"/> to <see cref="MaxDate"/> are greyed out and can't be picked.
/// </summary>
sealed class DayPicker : Control
{
    public event Action<DateOnly>? DatePicked;

    public Theme Theme { get; set; } = Theme.Current;
    public DateOnly MinDate { get; set; } = DateOnly.MinValue;
    public DateOnly MaxDate { get; set; } = DateOnly.MaxValue;
    /// <summary>The "today" ring and link: today at the place shown, which may differ from this PC's date.</summary>
    public DateOnly Today { get; set; } = DateOnly.FromDateTime(DateTime.Now);

    DateOnly month, selected;
    Rectangle rPrev, rNext, rToday, rPrevYear, rNextYear;
    readonly Dictionary<Rectangle, DateOnly> cells = [];
    Point hoverPt = new(-1, -1);

    public DayPicker()
    {
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint
                 | ControlStyles.ResizeRedraw | ControlStyles.Selectable, true);
        TabStop = true;
        selected = Today;
        month = new DateOnly(selected.Year, selected.Month, 1);
    }

    float S => DeviceDpi / 96f;
    int Px(float v) => (int)Math.Round(v * S);
    bool Rtl => RightToLeft == RightToLeft.Yes;

    public DateOnly Selected
    {
        get => selected;
        set { selected = Clamp(value); month = new DateOnly(selected.Year, selected.Month, 1); Invalidate(); }
    }

    DateOnly Clamp(DateOnly d) => d < MinDate ? MinDate : d > MaxDate ? MaxDate : d;

    void Shift(int months)
    {
        var m = month.AddMonths(months);
        if (m.AddMonths(1).AddDays(-1) < MinDate || m > MaxDate) return;
        month = m;
        Invalidate();
    }

    void Pick(DateOnly d)
    {
        if (d < MinDate || d > MaxDate) return;
        Selected = d;
        DatePicked?.Invoke(d);
    }

    protected override void OnMouseWheel(MouseEventArgs e) => Shift(e.Delta > 0 ? -1 : 1);

    protected override void OnMouseMove(MouseEventArgs e)
    {
        hoverPt = e.Location;
        bool hot = new[] { rPrev, rNext, rToday, rPrevYear, rNextYear }.Any(r => r.Contains(e.Location))
                   || cells.Any(c => c.Key.Contains(e.Location) && c.Value >= MinDate && c.Value <= MaxDate);
        Cursor = hot ? Cursors.Hand : Cursors.Default;
        Invalidate();
    }

    protected override void OnMouseLeave(EventArgs e) { hoverPt = new(-1, -1); Invalidate(); }

    protected override void OnMouseClick(MouseEventArgs e)
    {
        Focus();
        if (rPrev.Contains(e.Location)) Shift(Rtl ? 1 : -1);
        else if (rNext.Contains(e.Location)) Shift(Rtl ? -1 : 1);
        else if (rPrevYear.Contains(e.Location)) Shift(Rtl ? 12 : -12);
        else if (rNextYear.Contains(e.Location)) Shift(Rtl ? -12 : 12);
        else if (rToday.Contains(e.Location)) Pick(Today);
        else foreach (var (r, d) in cells) if (r.Contains(e.Location)) { Pick(d); return; }
    }

    protected override bool IsInputKey(Keys k) => k is Keys.Left or Keys.Right or Keys.Up or Keys.Down || base.IsInputKey(k);

    protected override void OnKeyDown(KeyEventArgs e)
    {
        int days = e.KeyCode switch { Keys.Left => Rtl ? 1 : -1, Keys.Right => Rtl ? -1 : 1, Keys.Up => -7, Keys.Down => 7, _ => 0 };
        if (days != 0) Pick(Clamp(selected.AddDays(days)));
        else if (e.KeyCode == Keys.PageUp) Shift(e.Control ? -12 : -1);
        else if (e.KeyCode == Keys.PageDown) Shift(e.Control ? 12 : 1);
        else if (e.KeyCode == Keys.Home) Pick(Today);
        base.OnKeyDown(e);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        var t = Theme;
        var culture = L.Culture;
        g.Clear(Styler.Backdrop(this, t.Window));
        var outer = new RectangleF(0.5f, 0.5f, Width - 1, Height - 1);
        using (var p = Art.RoundRect(outer, Px(10)))
        {
            using var b = new SolidBrush(t.Surface);
            g.FillPath(b, p);
            using var pen = new Pen(t.Border);
            g.DrawPath(pen, p);
        }

        // Header: « ‹  November 2026  › »  (double arrows jump a year).
        int bs = Px(28);
        rPrevYear = new Rectangle(Px(6), Px(8), bs, bs);
        rPrev = new Rectangle(Px(6) + bs, Px(8), bs, bs);
        rNext = new Rectangle(Width - Px(6) - bs * 2, Px(8), bs, bs);
        rNextYear = new Rectangle(Width - Px(6) - bs, Px(8), bs, bs);
        using var fIcon = new Font(Art.IconFontName, 9f);
        using var fTitle = new Font("Segoe UI Semibold", 10.5f);
        using var fSub = new Font("Segoe UI", 8f);
        using var fDay = new Font("Segoe UI", 9.5f);
        using var fDayB = new Font("Segoe UI Semibold", 9.5f);
        var center = TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding | (Rtl ? TextFormatFlags.RightToLeft : 0);
        foreach (var (r, glyph) in new[] { (rPrevYear, Art.IconPrevYear), (rPrev, Art.IconPrev), (rNext, Art.IconNext), (rNextYear, Art.IconNextYear) })
        {
            if (r.Contains(hoverPt)) Art.FillRound(g, r, Px(6), Color.FromArgb(t.Dark ? 30 : 18, t.Text));
            TextRenderer.DrawText(g, glyph, fIcon, r, t.Text, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
        }
        TextRenderer.DrawText(g, month.ToDateTime(TimeOnly.MinValue).ToString("MMMM yyyy", culture), fTitle,
            new Rectangle(rPrev.Right, Px(8), rNext.Left - rPrev.Right, bs), t.Text, center);

        // Weekday header: Saturday first in Arabic, Monday first in English (en-GB).
        var firstDow = Rtl ? DayOfWeek.Saturday : DayOfWeek.Monday;
        int gridLeft = Px(10), gridTop = Px(44), cw = (Width - Px(20)) / 7, ch = Px(34);
        for (int i = 0; i < 7; i++)
        {
            var dow = (DayOfWeek)(((int)firstDow + i) % 7);
            TextRenderer.DrawText(g, culture.DateTimeFormat.GetShortestDayName(dow), fSub, Cell(i, gridLeft, gridTop, cw, Px(18)), t.Dim, center);
        }

        cells.Clear();
        int offset = ((int)month.DayOfWeek - (int)firstDow + 7) % 7;
        var start = month.AddDays(-offset);
        int top = gridTop + Px(20);
        for (int i = 0; i < 42; i++)
        {
            var d = start.AddDays(i);
            var r = Cell(i % 7, gridLeft, top + (i / 7) * ch, cw, ch);
            cells[r] = d;
            bool usable = d >= MinDate && d <= MaxDate, inMonth = d.Month == month.Month;
            bool isSel = d == selected, isToday = d == Today;
            var inner = Rectangle.Inflate(r, -Px(2), -Px(2));
            if (isSel) Art.FillRound(g, inner, Px(8), t.AccentFill);
            else if (usable && r.Contains(hoverPt)) Art.FillRound(g, inner, Px(8), Color.FromArgb(t.Dark ? 28 : 16, t.Text));
            if (isToday && !isSel)
            {
                using var pen = new Pen(t.Accent, Math.Max(1.2f, 1.5f * S));
                using var p = Art.RoundRect(inner, Px(8));
                g.DrawPath(pen, p);
            }
            Color dc = isSel ? Color.White : !usable ? Color.FromArgb(70, t.Dim) : inMonth ? t.Text : t.Dim;
            TextRenderer.DrawText(g, d.Day.ToString(CultureInfo.InvariantCulture), isSel || isToday ? fDayB : fDay, r, dc, center);
        }

        rToday = new Rectangle(Width / 2 - Px(60), Height - Px(28), Px(120), Px(22));
        using var fLink = new Font("Segoe UI Semibold", 9f, rToday.Contains(hoverPt) ? FontStyle.Underline : FontStyle.Regular);
        TextRenderer.DrawText(g, L.T("Today"), fLink, rToday, t.Accent, center);
    }

    Rectangle Cell(int col, int left, int top, int cw, int ch) => new(left + (Rtl ? 6 - col : col) * cw, top, cw, ch);
}
