using System.Drawing.Drawing2D;

namespace SkyGlance.UI.Controls;

/// <summary>On/off switch in the Windows 11 style, teal when on.</summary>
sealed class ToggleSwitch : Control
{
    bool on, hover;
    public Theme Theme { get; set; } = Theme.Current;
    public event EventHandler? CheckedChanged;

    public ToggleSwitch()
    {
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint
                 | ControlStyles.SupportsTransparentBackColor | ControlStyles.Selectable, true);
        Size = new Size(44, 22);
        Cursor = Cursors.Hand;
        TabStop = true;
    }

    public bool Checked
    {
        get => on;
        set { if (on == value) return; on = value; Invalidate(); CheckedChanged?.Invoke(this, EventArgs.Empty); }
    }

    protected override void OnClick(EventArgs e) { Focus(); Checked = !Checked; base.OnClick(e); }
    protected override void OnKeyDown(KeyEventArgs e) { if (e.KeyCode == Keys.Space) Checked = !Checked; base.OnKeyDown(e); }
    protected override void OnMouseEnter(EventArgs e) { hover = true; Invalidate(); }
    protected override void OnMouseLeave(EventArgs e) { hover = false; Invalidate(); }
    protected override void OnEnabledChanged(EventArgs e) { base.OnEnabledChanged(e); Invalidate(); }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.Clear(Styler.Backdrop(this, Theme.Surface));
        float s = DeviceDpi / 96f;
        var track = new RectangleF(1, (Height - 20 * s) / 2, Width - 3, 20 * s);
        int alpha = Enabled ? 255 : 110;
        using (var p = Art.RoundRect(track, track.Height / 2))
        {
            if (on)
            {
                using var b = new SolidBrush(Color.FromArgb(alpha, hover ? Theme.Accent : Theme.AccentFill));
                g.FillPath(b, p);
            }
            else
            {
                using var pen = new Pen(Color.FromArgb(alpha, Theme.Dim), 1.2f * s);
                g.DrawPath(pen, p);
            }
        }
        float k = on ? 14 * s : 10 * s;
        bool right = on ^ (RightToLeft == RightToLeft.Yes);
        float cx = right ? track.Right - track.Height / 2 : track.Left + track.Height / 2;
        float cy = track.Top + track.Height / 2;
        using var kb = new SolidBrush(Color.FromArgb(alpha, on ? Color.White : Theme.Dim));
        g.FillEllipse(kb, cx - k / 2, cy - k / 2, k, k);
        if (Focused && ShowFocusCues)
        {
            using var fp = new Pen(Theme.Accent, 1) { DashStyle = DashStyle.Dot };
            using var p = Art.RoundRect(RectangleF.Inflate(track, 0, 0), track.Height / 2);
            g.DrawPath(fp, p);
        }
    }
}
