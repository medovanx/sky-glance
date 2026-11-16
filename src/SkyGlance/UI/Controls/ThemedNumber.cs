using System.Drawing.Drawing2D;
using System.Globalization;

namespace SkyGlance.UI.Controls;

/// <summary>Number box drawn in the app theme: [−] value [+], typing, arrow keys and mouse wheel.</summary>
sealed class ThemedNumber : Control
{
    readonly TextBox text = new() { BorderStyle = BorderStyle.None, TextAlign = HorizontalAlignment.Center };
    decimal value;
    Rectangle rMinus, rPlus;
    int hot; // -1 minus, 1 plus
    public Theme Theme { get; set; } = Theme.Current;
    public decimal Minimum { get; set; }
    public decimal Maximum { get; set; } = 100;
    public int DecimalPlaces { get; set; }
    public decimal Increment { get; set; } = 1;
    public event EventHandler? ValueChanged;

    public ThemedNumber()
    {
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
        Size = new Size(130, 30);
        Controls.Add(text);
        text.Leave += (_, _) => Commit();
        text.KeyDown += (_, e) =>
        {
            if (e.KeyCode == Keys.Enter) { Commit(); e.SuppressKeyPress = true; }
            else if (e.KeyCode == Keys.Up) { Step(1); e.SuppressKeyPress = true; }
            else if (e.KeyCode == Keys.Down) { Step(-1); e.SuppressKeyPress = true; }
        };
        text.MouseWheel += (_, e) => Step(e.Delta > 0 ? 1 : -1);
        text.GotFocus += (_, _) => Invalidate();
        text.LostFocus += (_, _) => Invalidate();
        text.Text = "0";
    }

    public decimal Value
    {
        get => value;
        set
        {
            var v = Math.Round(Math.Clamp(value, Minimum, Maximum), DecimalPlaces);
            bool changed = v != this.value;
            this.value = v;
            text.Text = v.ToString("F" + DecimalPlaces, CultureInfo.InvariantCulture);
            if (changed) ValueChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    void Commit()
    {
        if (decimal.TryParse(text.Text.Trim(), NumberStyles.Number, CultureInfo.InvariantCulture, out var v)) Value = v;
        else Value = value; // restore the last good value
    }

    void Step(int dir) { Commit(); Value = value + dir * Increment; }

    protected override void OnMouseWheel(MouseEventArgs e) => Step(e.Delta > 0 ? 1 : -1);

    protected override void OnLayout(LayoutEventArgs e)
    {
        base.OnLayout(e);
        int b = Height;
        rMinus = new Rectangle(0, 0, b, Height);
        rPlus = new Rectangle(Width - b, 0, b, Height);
        text.Font = Font;
        text.Width = Math.Max(10, Width - 2 * b - 4);
        text.Location = new Point(b + 2, (Height - text.PreferredHeight) / 2 + 1);
        ApplyColors();
    }

    void ApplyColors()
    {
        text.BackColor = Theme.Input;
        text.ForeColor = Enabled ? Theme.Text : Theme.Dim;
    }

    protected override void OnEnabledChanged(EventArgs e) { base.OnEnabledChanged(e); text.Enabled = Enabled; ApplyColors(); Invalidate(); }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        int h = rMinus.Contains(e.Location) ? -1 : rPlus.Contains(e.Location) ? 1 : 0;
        Cursor = h != 0 ? Cursors.Hand : Cursors.Default;
        if (h != hot) { hot = h; Invalidate(); }
    }

    protected override void OnMouseLeave(EventArgs e) { hot = 0; Invalidate(); }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        if (!Enabled || e.Button != MouseButtons.Left) return;
        if (rMinus.Contains(e.Location)) Step(-1);
        else if (rPlus.Contains(e.Location)) Step(1);
        else text.Focus();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.Clear(Styler.Backdrop(this, Theme.Surface));
        ApplyColors();
        float s = DeviceDpi / 96f;
        var box = new RectangleF(0.5f, 0.5f, Width - 1, Height - 1);
        using (var p = Art.RoundRect(box, 6 * s))
        {
            using var b = new SolidBrush(Theme.Input);
            g.FillPath(b, p);
            using var pen = new Pen(text.Focused ? Theme.Accent : Theme.Border, text.Focused ? 1.4f * s : 1f);
            g.DrawPath(pen, p);
        }
        using var f = new Font(Art.IconFontName, 8f);
        Color c = Enabled ? Theme.Text : Theme.Dim;
        foreach (var (r, glyph, id) in new[] { (rMinus, "\uE738", -1), (rPlus, "\uE710", 1) })
        {
            if (hot == id && Enabled)
            {
                using var hp = Art.RoundRect(Rectangle.Inflate(r, -(int)(3 * s), -(int)(3 * s)), 4 * s);
                using var hb = new SolidBrush(Color.FromArgb(Theme.Dark ? 36 : 20, Theme.Accent));
                g.FillPath(hb, hp);
            }
            TextRenderer.DrawText(g, glyph, f, r, c, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
        }
    }
}
