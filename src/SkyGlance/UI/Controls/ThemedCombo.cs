using System.Drawing.Drawing2D;
using System.Globalization;

namespace SkyGlance.UI.Controls;

/// <summary>Dropdown drawn in the app theme, with its own themed list popup (the stock ComboBox ignores colours).</summary>
sealed class ThemedCombo : Control
{
    public List<string> Items { get; } = [];
    public Theme Theme { get; set; } = Theme.Current;
    public event EventHandler? SelectedIndexChanged;

    int selected = -1;
    bool hover, open;
    ToolStripDropDown? popup;

    public ThemedCombo()
    {
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint
                 | ControlStyles.ResizeRedraw | ControlStyles.Selectable, true);
        Size = new Size(220, 30);
        Cursor = Cursors.Hand;
        TabStop = true;
    }

    public int SelectedIndex
    {
        get => selected;
        set
        {
            if (value < -1 || value >= Items.Count || value == selected) return;
            selected = value;
            Invalidate();
            SelectedIndexChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    /// <summary>Setting true opens the list (used after a city search).</summary>
    public bool DroppedDown
    {
        get => open;
        set { if (value && !open) Open(); else if (!value) popup?.Close(); }
    }

    protected override void OnMouseEnter(EventArgs e) { hover = true; Invalidate(); }
    protected override void OnMouseLeave(EventArgs e) { hover = false; Invalidate(); }
    protected override void OnGotFocus(EventArgs e) { base.OnGotFocus(e); Invalidate(); }
    protected override void OnLostFocus(EventArgs e) { base.OnLostFocus(e); Invalidate(); }
    protected override void OnEnabledChanged(EventArgs e) { base.OnEnabledChanged(e); Invalidate(); }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);
        Focus();
        if (e.Button == MouseButtons.Left && Items.Count > 0) Open();
    }

    protected override bool IsInputKey(Keys k) => k is Keys.Up or Keys.Down || base.IsInputKey(k);

    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Down && e.Alt || e.KeyCode is Keys.F4 or Keys.Space or Keys.Enter) Open();
        else if (e.KeyCode == Keys.Down) SelectedIndex = Math.Min(Items.Count - 1, selected + 1);
        else if (e.KeyCode == Keys.Up) SelectedIndex = Math.Max(0, selected - 1);
        base.OnKeyDown(e);
    }

    protected override void OnMouseWheel(MouseEventArgs e)
    {
        if (!Focused || Items.Count == 0) return;
        SelectedIndex = Math.Clamp(selected + (e.Delta > 0 ? -1 : 1), 0, Items.Count - 1);
    }

    void Open()
    {
        if (open || Items.Count == 0) return;
        var list = new ThemedList(this) { Theme = Theme, RightToLeft = RightToLeft, Font = Font };
        int rowH = (int)(30 * DeviceDpi / 96f);
        int visible = Math.Min(Items.Count, 10);
        list.Size = new Size(Width, rowH * visible + 2);
        var host = new ToolStripControlHost(list) { Margin = Padding.Empty, Padding = Padding.Empty, AutoSize = false, Size = list.Size };
        popup = new ToolStripDropDown { Padding = Padding.Empty, DropShadowEnabled = true, BackColor = Theme.Surface, Renderer = new BorderRenderer(Theme) };
        popup.Items.Add(host);
        popup.Closed += (_, _) =>
        {
            open = false;
            Invalidate();
            // WinForms' menu filter still touches the dropdown after Closed: dispose it once that is done,
            // or the next click crashes the app with ObjectDisposedException.
            var closing = popup;
            popup = null;
            if (closing != null && IsHandleCreated) BeginInvoke(closing.Dispose);
        };
        open = true;
        Invalidate();
        popup.Show(this, new Point(0, Height + 2));
        list.Focus();
    }

    internal void Pick(int index)
    {
        SelectedIndex = index;
        popup?.Close();
        Focus();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.Clear(Styler.Backdrop(this, Theme.Surface));
        float s = DeviceDpi / 96f;
        bool rtl = RightToLeft == RightToLeft.Yes;
        var box = new RectangleF(0.5f, 0.5f, Width - 1, Height - 1);
        using (var p = Art.RoundRect(box, 6 * s))
        {
            using var b = new SolidBrush(hover && Enabled ? Color.FromArgb(Theme.Dark ? 60 : 238, Theme.Dark ? 64 : 241, Theme.Dark ? 64 : 240) : Theme.Input);
            g.FillPath(b, p);
            using var pen = new Pen(open || Focused ? Theme.Accent : Theme.Border, open || Focused ? 1.4f * s : 1f);
            g.DrawPath(pen, p);
        }

        Color fg = Enabled ? Theme.Text : Theme.Dim;
        int chevW = (int)(28 * s), pad = (int)(10 * s);
        var chev = new Rectangle(rtl ? 0 : Width - chevW, 0, chevW, Height);
        using (var f = new Font(Art.IconFontName, 8f))
            TextRenderer.DrawText(g, "\uE70D", f, chev, Theme.Dim, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
        if (selected >= 0)
        {
            var tr = rtl ? new Rectangle(chevW, 0, Width - chevW - pad, Height) : new Rectangle(pad, 0, Width - chevW - pad, Height);
            TextRenderer.DrawText(g, Items[selected], Font, tr, fg, TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.SingleLine
                | (rtl ? TextFormatFlags.Right | TextFormatFlags.RightToLeft : TextFormatFlags.Left));
        }
    }

    sealed class BorderRenderer(Theme t) : ToolStripRenderer
    {
        protected override void OnRenderToolStripBorder(ToolStripRenderEventArgs e)
        {
            using var pen = new Pen(t.Border);
            e.Graphics.DrawRectangle(pen, 0, 0, e.ToolStrip.Width - 1, e.ToolStrip.Height - 1);
        }
        protected override void OnRenderToolStripBackground(ToolStripRenderEventArgs e) => e.Graphics.Clear(t.Surface);
    }

    /// <summary>The list shown inside the popup.</summary>
    sealed class ThemedList(ThemedCombo owner) : Control
    {
        public Theme Theme { get; set; } = Theme.Current;
        int hover = -1, top;

        int RowH => (int)(30 * DeviceDpi / 96f);
        int VisibleRows => Math.Max(1, (Height - 2) / RowH);

        protected override void OnCreateControl()
        {
            base.OnCreateControl();
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint, true);
            hover = owner.selected;
            top = Math.Clamp(owner.selected - VisibleRows + 1, 0, Math.Max(0, owner.Items.Count - VisibleRows));
        }

        int IndexAt(Point p) { int i = top + (p.Y - 1) / RowH; return i >= 0 && i < owner.Items.Count ? i : -1; }

        protected override void OnMouseMove(MouseEventArgs e) { int i = IndexAt(e.Location); if (i != hover) { hover = i; Invalidate(); } }
        protected override void OnMouseClick(MouseEventArgs e) { int i = IndexAt(e.Location); if (i >= 0) owner.Pick(i); }
        protected override void OnMouseWheel(MouseEventArgs e)
        {
            top = Math.Clamp(top + (e.Delta > 0 ? -1 : 1), 0, Math.Max(0, owner.Items.Count - VisibleRows));
            Invalidate();
        }
        protected override bool IsInputKey(Keys k) => k is Keys.Up or Keys.Down or Keys.Enter || base.IsInputKey(k);
        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Down) hover = Math.Min(owner.Items.Count - 1, hover + 1);
            else if (e.KeyCode == Keys.Up) hover = Math.Max(0, hover - 1);
            else if (e.KeyCode == Keys.Enter && hover >= 0) { owner.Pick(hover); return; }
            if (hover < top) top = hover;
            if (hover >= top + VisibleRows) top = hover - VisibleRows + 1;
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(Theme.Surface);
            float s = DeviceDpi / 96f;
            bool rtl = RightToLeft == RightToLeft.Yes;
            for (int i = top; i < Math.Min(owner.Items.Count, top + VisibleRows); i++)
            {
                var r = new Rectangle(1, 1 + (i - top) * RowH, Width - 2, RowH);
                var inner = Rectangle.Inflate(r, -(int)(4 * s), -(int)(2 * s));
                bool sel = i == owner.selected;
                if (i == hover || sel)
                {
                    using var p = Art.RoundRect(inner, 5 * s);
                    using var b = new SolidBrush(i == hover ? Color.FromArgb(Theme.Dark ? 40 : 24, Theme.Accent) : Color.FromArgb(Theme.Dark ? 22 : 12, Theme.Text));
                    g.FillPath(b, p);
                }
                if (sel)
                {
                    using var ab = new SolidBrush(Theme.Accent);
                    g.FillRectangle(ab, rtl ? inner.Right - (int)(3 * s) : inner.Left, inner.Top + (int)(7 * s), (int)(3 * s), inner.Height - (int)(14 * s));
                }
                var tr = Rectangle.Inflate(inner, -(int)(10 * s), 0);
                TextRenderer.DrawText(g, owner.Items[i], Font, tr, sel ? Theme.Accent : Theme.Text,
                    TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.SingleLine
                    | (rtl ? TextFormatFlags.Right | TextFormatFlags.RightToLeft : TextFormatFlags.Left));
            }
        }
    }
}
