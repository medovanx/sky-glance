using System.Drawing.Drawing2D;

namespace SkyGlance.UI.Settings;

/// <summary>Settings sidebar: brand header with a cloud pattern and the page list with a blue selection pill.</summary>
sealed class NavList : Control
{
    readonly (string glyph, string title)[] items;
    readonly Theme t;
    int selected, hover = -1;
    public event Action<int>? SelectedChanged;

    public NavList((string, string)[] items, Theme t)
    {
        this.items = items;
        this.t = t;
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
        Cursor = Cursors.Hand;
    }

    public int Selected { get => selected; set { selected = value; Invalidate(); } }

    float S => DeviceDpi / 96f;
    Rectangle Item(int i) => new((int)(10 * S), (int)((112 + i * 44) * S), Width - (int)(20 * S), (int)(40 * S));

    protected override void OnMouseMove(MouseEventArgs e)
    {
        int h = Enumerable.Range(0, items.Length).FirstOrDefault(i => Item(i).Contains(e.Location), -1);
        if (h != hover) { hover = h; Invalidate(); }
    }

    protected override void OnMouseLeave(EventArgs e) { hover = -1; Invalidate(); }

    protected override void OnMouseClick(MouseEventArgs e)
    {
        for (int i = 0; i < items.Length; i++)
            if (Item(i).Contains(e.Location)) { selected = i; Invalidate(); SelectedChanged?.Invoke(i); }
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        bool rtl = RightToLeft == RightToLeft.Yes;

        var head = new Rectangle(0, 0, Width, (int)(92 * S));
        using (var grad = new LinearGradientBrush(head, Art.SkyLight, Art.SkyDark, LinearGradientMode.ForwardDiagonal))
            g.FillRectangle(grad, head);
        Art.CloudPattern(g, head, 44 * S, Color.FromArgb(22, 255, 255, 255));
        using (var below = new SolidBrush(t.Dark ? Color.FromArgb(24, 27, 30) : Color.FromArgb(235, 238, 242)))
            g.FillRectangle(below, 0, head.Bottom, Width, Height - head.Bottom);

        int gs = (int)(36 * S), gx = rtl ? Width - (int)(16 * S) - gs : (int)(16 * S);
        Art.Mark(g, new RectangleF(gx, (head.Height - gs) / 2f, gs, gs));
        var textRect = rtl ? new Rectangle((int)(10 * S), 0, gx - (int)(20 * S), head.Height)
                           : new Rectangle(gx + gs + (int)(10 * S), 0, Width - gx - gs - (int)(20 * S), head.Height);
        var tf = TextFormatFlags.VerticalCenter | (rtl ? TextFormatFlags.Right | TextFormatFlags.RightToLeft : TextFormatFlags.Left);
        using var fName = new Font("Segoe UI Semibold", 12f);
        using var fVer = new Font("Segoe UI", 8.5f);
        TextRenderer.DrawText(g, L.T("AppName"), fName, new Rectangle(textRect.X, textRect.Y - (int)(9 * S), textRect.Width, textRect.Height), Color.White, tf);
        TextRenderer.DrawText(g, "v" + Updater.CurrentText, fVer, new Rectangle(textRect.X, textRect.Y + (int)(12 * S), textRect.Width, textRect.Height), Color.FromArgb(215, 232, 245), tf);

        using var fIcon = new Font(Art.IconFontName, 11f);
        using var fItem = new Font("Segoe UI", 10f);
        using var fItemB = new Font("Segoe UI Semibold", 10f);
        for (int i = 0; i < items.Length; i++)
        {
            var r = Item(i);
            if (i == selected || i == hover)
            {
                using var p = Art.RoundRect(r, 6 * S);
                using var b = new SolidBrush(i == selected ? Color.FromArgb(t.Dark ? 44 : 30, t.Accent) : Color.FromArgb(t.Dark ? 22 : 14, t.Text));
                g.FillPath(b, p);
            }
            if (i == selected)
            {
                var bar = new Rectangle(rtl ? r.Right - (int)(3 * S) : r.Left, r.Top + (int)(11 * S), (int)(3 * S), r.Height - (int)(22 * S));
                using var bb = new SolidBrush(t.Accent);
                g.FillRectangle(bb, bar);
            }
            int ix = rtl ? r.Right - (int)(40 * S) : r.Left + (int)(12 * S);
            var ir = new Rectangle(ix, r.Top, (int)(28 * S), r.Height);
            TextRenderer.DrawText(g, items[i].glyph, fIcon, ir, i == selected ? t.Accent : t.Dim, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            var lr = rtl ? new Rectangle(r.Left + (int)(8 * S), r.Top, r.Width - (int)(56 * S), r.Height)
                         : new Rectangle(r.Left + (int)(48 * S), r.Top, r.Width - (int)(56 * S), r.Height);
            TextRenderer.DrawText(g, items[i].title, i == selected ? fItemB : fItem, lr, t.Text, tf | TextFormatFlags.EndEllipsis);
        }
    }
}
