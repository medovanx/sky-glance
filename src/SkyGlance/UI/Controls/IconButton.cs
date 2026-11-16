using System.Drawing.Drawing2D;

namespace SkyGlance.UI.Controls;

/// <summary>Flat button with an icon-font glyph and text centred together as one group.</summary>
sealed class IconButton : Control
{
    bool hover, down;
    public string Glyph { get; set; } = "";
    public Theme Theme { get; set; } = Theme.Current;
    public bool Primary { get; set; }

    public IconButton()
    {
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
        Cursor = Cursors.Hand;
        Font = new Font("Segoe UI", 9f);
    }

    protected override void OnMouseEnter(EventArgs e) { hover = true; Invalidate(); }
    protected override void OnMouseLeave(EventArgs e) { hover = down = false; Invalidate(); }
    protected override void OnMouseDown(MouseEventArgs e) { down = true; Invalidate(); base.OnMouseDown(e); }
    protected override void OnMouseUp(MouseEventArgs e) { down = false; Invalidate(); base.OnMouseUp(e); }
    protected override void OnTextChanged(EventArgs e) { base.OnTextChanged(e); Invalidate(); }
    protected override void OnEnabledChanged(EventArgs e) { base.OnEnabledChanged(e); Invalidate(); }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.Clear(Styler.Backdrop(this, Theme.Window));
        float s = DeviceDpi / 96f;
        Color bg = Primary ? (hover ? Theme.AccentHover : Theme.AccentFill)
                 : Theme.Dark ? (down ? Color.FromArgb(40, 43, 43) : hover ? Color.FromArgb(58, 62, 62) : Color.FromArgb(46, 49, 49))
                 : (down ? Color.FromArgb(232, 235, 234) : hover ? Color.FromArgb(242, 245, 244) : Color.White);
        Color fg = Primary ? Color.White : Theme.Text;
        if (!Enabled) fg = Color.FromArgb(120, fg);
        using (var p = Art.RoundRect(new RectangleF(0.5f, 0.5f, Width - 1, Height - 1), 6 * s))
        {
            using var b = new SolidBrush(bg);
            g.FillPath(b, p);
            if (!Primary) { using var pen = new Pen(Theme.Border); g.DrawPath(pen, p); }
        }

        bool rtl = RightToLeft == RightToLeft.Yes;
        using var fIcon = new Font(Art.IconFontName, Font.SizeInPoints + 1.5f);
        var flags = TextFormatFlags.NoPadding | TextFormatFlags.SingleLine | (rtl ? TextFormatFlags.RightToLeft : 0);
        Size it = Glyph.Length > 0 ? TextRenderer.MeasureText(g, Glyph, fIcon, Size.Empty, TextFormatFlags.NoPadding) : Size.Empty;
        Size tt = TextRenderer.MeasureText(g, Text, Font, Size.Empty, flags);
        int gap = Glyph.Length > 0 ? (int)(8 * s) : 0;
        int total = it.Width + gap + tt.Width;
        int x = (Width - total) / 2;
        int iconX = rtl ? x + tt.Width + gap : x;
        int textX = rtl ? x : x + it.Width + gap;
        if (Glyph.Length > 0)
            TextRenderer.DrawText(g, Glyph, fIcon, new Rectangle(iconX, 0, it.Width, Height), fg,
                TextFormatFlags.NoPadding | TextFormatFlags.VerticalCenter | TextFormatFlags.HorizontalCenter);
        TextRenderer.DrawText(g, Text, Font, new Rectangle(textX, 0, tt.Width + 2, Height), fg, flags | TextFormatFlags.VerticalCenter);
    }
}
