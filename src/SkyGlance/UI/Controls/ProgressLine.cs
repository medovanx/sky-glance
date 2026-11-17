using System.Drawing.Drawing2D;

namespace SkyGlance.UI.Controls;

/// <summary>
/// A slim rounded progress bar in the accent colour. A null <see cref="Value"/> shows a sliding segment
/// for work with no known end (checking, verifying); it animates only while visible.
/// </summary>
sealed class ProgressLine : Control
{
    readonly System.Windows.Forms.Timer anim = new() { Interval = 16 };
    double? value;
    float phase;

    public Theme Theme { get; set; } = Theme.Current;

    public ProgressLine()
    {
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint
                 | ControlStyles.SupportsTransparentBackColor, true);
        Size = new Size(200, 32);
        anim.Tick += (_, _) => { phase = (phase + 0.012f) % 1f; Invalidate(); };
    }

    /// <summary>0 to 1, or null for an indeterminate bar.</summary>
    public double? Value
    {
        get => value;
        set { this.value = value is { } v ? Math.Clamp(v, 0, 1) : null; SyncTimer(); Invalidate(); }
    }

    void SyncTimer() => anim.Enabled = value == null && Visible && IsHandleCreated;

    protected override void OnVisibleChanged(EventArgs e) { base.OnVisibleChanged(e); SyncTimer(); }
    protected override void OnHandleCreated(EventArgs e) { base.OnHandleCreated(e); SyncTimer(); }

    protected override void Dispose(bool disposing)
    {
        if (disposing) anim.Dispose();
        base.Dispose(disposing);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.Clear(Styler.Backdrop(this, Theme.Surface));
        float s = DeviceDpi / 96f, h = 6 * s;
        var track = new RectangleF(1, (Height - h) / 2, Width - 2, h);
        Art.FillRound(g, track, h / 2, Color.FromArgb(Theme.Dark ? 46 : 30, Theme.Text));
        RectangleF fill;
        if (value is { } v)
        {
            if (v <= 0) return;
            fill = new RectangleF(track.X, track.Y, Math.Max(h, track.Width * (float)v), h);
        }
        else
        {
            // A segment a third of the track wide, eased so it speeds up in the middle.
            float seg = track.Width / 3, t = (1 - MathF.Cos(phase * MathF.PI)) / 2;
            float x = track.X - seg + (track.Width + seg) * t;
            float l = Math.Max(track.X, x), r = Math.Min(track.Right, x + seg);
            if (r - l < 1) return;
            fill = new RectangleF(l, track.Y, r - l, h);
        }
        if (RightToLeft == RightToLeft.Yes) fill.X = track.Right - (fill.X - track.X) - fill.Width;
        Art.FillRound(g, fill, h / 2, Theme.Accent);
    }
}
