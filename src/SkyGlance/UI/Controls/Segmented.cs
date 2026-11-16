using System.Drawing.Drawing2D;

namespace SkyGlance.UI.Controls;

/// <summary>Two or more pill options side by side; one is selected.</summary>
sealed class Segmented : Control
{
    readonly string[] options;
    int selected, hover = -1;
    public Theme Theme { get; set; } = Theme.Current;
    public event EventHandler? SelectedChanged;

    public Segmented(params string[] options)
    {
        this.options = options;
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint, true);
        Height = 34;
        Cursor = Cursors.Hand;
    }

    public int Selected
    {
        get => selected;
        set { if (selected == value) return; selected = value; Invalidate(); SelectedChanged?.Invoke(this, EventArgs.Empty); }
    }

    Rectangle Seg(int i)
    {
        int w = (Width - 6) / options.Length;
        int idx = RightToLeft == RightToLeft.Yes ? options.Length - 1 - i : i;
        return new Rectangle(3 + idx * w, 3, w, Height - 6);
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        int h = Enumerable.Range(0, options.Length).FirstOrDefault(i => Seg(i).Contains(e.Location), -1);
        if (h != hover) { hover = h; Invalidate(); }
    }

    protected override void OnMouseLeave(EventArgs e) { hover = -1; Invalidate(); }

    protected override void OnMouseClick(MouseEventArgs e)
    {
        for (int i = 0; i < options.Length; i++) if (Seg(i).Contains(e.Location)) Selected = i;
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.Clear(Styler.Backdrop(this, Theme.Surface));
        float s = DeviceDpi / 96f;
        using (var p = Art.RoundRect(new RectangleF(0.5f, 0.5f, Width - 1, Height - 1), 8 * s))
        using (var b = new SolidBrush(Theme.Input))
        using (var pen = new Pen(Theme.Border))
        {
            g.FillPath(b, p);
            g.DrawPath(pen, p);
        }
        for (int i = 0; i < options.Length; i++)
        {
            var r = Seg(i);
            if (i == selected || i == hover)
            {
                using var p = Art.RoundRect(r, 6 * s);
                using var b = new SolidBrush(i == selected ? Theme.AccentFill : Color.FromArgb(Theme.Dark ? 30 : 18, Theme.Text));
                g.FillPath(b, p);
            }
            var flags = TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis
                        | (RightToLeft == RightToLeft.Yes ? TextFormatFlags.RightToLeft : 0);
            using var f = new Font(Font, i == selected ? FontStyle.Bold : FontStyle.Regular);
            TextRenderer.DrawText(g, options[i], f, r, i == selected ? Color.White : Theme.Text, flags);
        }
    }
}
