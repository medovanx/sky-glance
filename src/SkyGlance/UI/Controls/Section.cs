using System.Drawing.Drawing2D;

namespace SkyGlance.UI.Controls;

/// <summary>A rounded "card" with a title, holding a settings group.</summary>
sealed class Section : Panel
{
    public Theme Theme { get; set; } = Theme.Current;
    public string Glyph { get; set; } = "";

    public Section(string title)
    {
        Text = title;
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
        Padding = new Padding(16, 44, 16, 12);
        AutoSize = true;
        AutoSizeMode = AutoSizeMode.GrowAndShrink;
    }

    protected override void OnPaintBackground(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.Clear(Styler.Backdrop(this, Theme.Window));
        float s = DeviceDpi / 96f;
        using (var p = Art.RoundRect(new RectangleF(0.5f, 0.5f, Width - 1, Height - 1), 10 * s))
        using (var b = new SolidBrush(Theme.Surface))
        using (var pen = new Pen(Theme.Border))
        {
            g.FillPath(b, p);
            g.DrawPath(pen, p);
        }
        bool rtl = RightToLeft == RightToLeft.Yes;
        int x = (int)(16 * s), y = (int)(12 * s), gs = (int)(20 * s);
        var glyphRect = new Rectangle(rtl ? Width - x - gs : x, y, gs, gs);
        if (Glyph.Length > 0)
        {
            using var gf = new Font(Art.IconFontName, 11f);
            TextRenderer.DrawText(g, Glyph, gf, glyphRect, Theme.Accent, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
        }
        using var tf = new Font("Segoe UI Semibold", 10.5f);
        int tx = x + gs + (int)(8 * s);
        var tr = rtl ? new Rectangle(x, y, Width - tx - x, gs) : new Rectangle(tx, y, Width - tx - x, gs);
        TextRenderer.DrawText(g, Text, tf, tr, Theme.Text, TextFormatFlags.VerticalCenter
            | (rtl ? TextFormatFlags.Right | TextFormatFlags.RightToLeft : TextFormatFlags.Left));
    }
}
