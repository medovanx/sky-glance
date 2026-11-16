using System.Drawing.Drawing2D;
using System.Globalization;

namespace SkyGlance.UI.Controls;

/// <summary>Single-line text box with a rounded themed border (the stock border can't be recoloured).</summary>
sealed class ThemedText : Control
{
    readonly TextBox box = new() { BorderStyle = BorderStyle.None };
    public Theme Theme { get; set; } = Theme.Current;

    public ThemedText()
    {
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
        Size = new Size(260, 30);
        Controls.Add(box);
        box.KeyDown += (_, e) => OnKeyDown(e);
        box.TextChanged += (_, e) => OnTextChanged(e);
        box.GotFocus += (_, _) => Invalidate();
        box.LostFocus += (_, _) => Invalidate();
        Cursor = Cursors.IBeam;
    }

#pragma warning disable CS8765 // Text is never null here
    public override string Text { get => box.Text; set => box.Text = value ?? ""; }
#pragma warning restore CS8765
    public string PlaceholderText { get => box.PlaceholderText; set => box.PlaceholderText = value; }

    protected override void OnMouseDown(MouseEventArgs e) { base.OnMouseDown(e); box.Focus(); }
    protected override void OnEnabledChanged(EventArgs e) { base.OnEnabledChanged(e); box.Enabled = Enabled; Invalidate(); }

    protected override void OnLayout(LayoutEventArgs e)
    {
        base.OnLayout(e);
        int pad = (int)(10 * DeviceDpi / 96f);
        box.Font = Font;
        box.RightToLeft = RightToLeft;
        box.Width = Math.Max(10, Width - pad * 2);
        box.Location = new Point(pad, (Height - box.PreferredHeight) / 2 + 1);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.Clear(Styler.Backdrop(this, Theme.Surface));
        box.BackColor = Theme.Input;
        box.ForeColor = Enabled ? Theme.Text : Theme.Dim;
        float s = DeviceDpi / 96f;
        using var p = Art.RoundRect(new RectangleF(0.5f, 0.5f, Width - 1, Height - 1), 6 * s);
        using var b = new SolidBrush(Theme.Input);
        g.FillPath(b, p);
        using var pen = new Pen(box.Focused ? Theme.Accent : Theme.Border, box.Focused ? 1.4f * s : 1f);
        g.DrawPath(pen, p);
    }
}
