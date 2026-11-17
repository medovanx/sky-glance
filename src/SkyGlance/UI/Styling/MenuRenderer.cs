using System.Drawing.Drawing2D;

namespace SkyGlance.UI.Styling;

/// <summary>
/// Menus in the app's own style, like the flyout and the sibling apps: the theme surface, rounded hover rows
/// inset from the edges, an accent check, dim separators and submenu arrows, and an icon-font glyph beside an
/// item whose <see cref="ToolStripItem.Tag"/> is a <see cref="MenuGlyph"/>.
/// </summary>
sealed class MenuRenderer(Theme t) : ToolStripRenderer
{
    /// <summary>Put in an item's Tag to draw this icon-font glyph in the image margin.</summary>
    public sealed record MenuGlyph(string Glyph, Color? Color = null);

    const string Rounded = "SkyGlance.Rounded";
    readonly Dictionary<ToolStrip, Rectangle> margins = [];

    public bool Dark => t.Dark;

    /// <summary>
    /// Themes <paramref name="menu"/> and every submenu, rounds their corners and gives the rows room.
    /// Call again after rebuilding the items or when the theme changes.
    /// </summary>
    public static void Apply(ToolStripDropDown menu, Theme theme)
    {
        var r = new MenuRenderer(theme);
        float s = menu.DeviceDpi / 96f;
        void Style(ToolStripDropDown d)
        {
            d.Renderer = r;
            d.Padding = new Padding(0, (int)(4 * s), 0, (int)(4 * s));
            d.Font = new Font("Segoe UI", 9.5f);
            d.BackColor = theme.Surface;
            d.ForeColor = theme.Text;
            if (d is ToolStripDropDownMenu dm) dm.ImageScalingSize = new Size((int)(16 * s), (int)(16 * s));
            // Subscribe once per menu; the handler reads the theme the menu has now.
            if (!Equals(d.AccessibleDescription, Rounded))
            {
                d.AccessibleDescription = Rounded;
                d.Opened += (_, _) => { if (d.IsHandleCreated) Native.RoundCorners(d.Handle, (d.Renderer as MenuRenderer)?.Dark ?? true); };
            }
            foreach (ToolStripItem it in d.Items)
            {
                if (it is ToolStripSeparator) continue;
                it.Padding = new Padding(0, (int)(4 * s), 0, (int)(4 * s));
                if (it is ToolStripMenuItem { HasDropDownItems: true } mi) Style(mi.DropDown);
            }
        }
        Style(menu);
    }

    protected override void OnRenderToolStripBackground(ToolStripRenderEventArgs e)
    {
        using var b = new SolidBrush(t.Surface);
        e.Graphics.FillRectangle(b, e.AffectedBounds);
    }

    // Windows draws the rounded border (RoundCorners); a square one would poke out of the corners.
    protected override void OnRenderToolStripBorder(ToolStripRenderEventArgs e) { }

    protected override void OnRenderImageMargin(ToolStripRenderEventArgs e) => margins[e.ToolStrip] = e.AffectedBounds;

    protected override void OnRenderMenuItemBackground(ToolStripItemRenderEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        var it = e.Item;
        float s = it.Owner?.DeviceDpi / 96f ?? 1f;
        if (it.Selected && it.Enabled)
            Art.FillRound(g, new RectangleF(4 * s, 1 * s, it.Width - 8 * s, it.Height - 2 * s), 5 * s, Color.FromArgb(t.Dark ? 40 : 24, t.Accent));
    }

    protected override void OnRenderItemCheck(ToolStripItemImageRenderEventArgs e)
    {
        float s = e.Item.Owner?.DeviceDpi / 96f ?? 1f;
        Art.GlyphCentered(e.Graphics, "", 11 * s, e.ImageRectangle, t.Accent);
    }

    protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
    {
        var it = e.Item;
        e.TextColor = !it.Enabled ? t.Dim : it is ToolStripMenuItem { Checked: true } ? t.Accent : t.Text;
        base.OnRenderItemText(e);
        DrawGlyph(e.Graphics, it, e.TextRectangle);
    }

    /// <summary>
    /// The item's icon in the image margin, centred on the text line rather than the row:
    /// the menu sets its text a few pixels above the row's middle.
    /// </summary>
    void DrawGlyph(Graphics g, ToolStripItem it, Rectangle text)
    {
        if (it.Tag is not MenuGlyph mg || it.Owner is not { } owner || !margins.TryGetValue(owner, out var m) || m.Width <= 0) return;
        float s = owner.DeviceDpi / 96f;
        bool rtl = owner.RightToLeft == RightToLeft.Yes;
        float w = m.Width;
        // The ink of a text line sits a little below its box's middle (room above for accents), so nudge down.
        var box = new RectangleF(rtl ? it.Width - w : 0, text.Top + 1 * s, w, text.Height);
        box.Offset(rtl ? -2 * s : 2 * s, 0);
        Art.GlyphCentered(g, mg.Glyph, 14 * s, box, !it.Enabled ? Color.FromArgb(120, t.Dim) : mg.Color ?? t.Dim);
    }

    protected override void OnRenderSeparator(ToolStripSeparatorRenderEventArgs e)
    {
        float s = e.Item.Owner?.DeviceDpi / 96f ?? 1f;
        int y = e.Item.Height / 2;
        using var pen = new Pen(t.Border, Math.Max(1, s));
        e.Graphics.DrawLine(pen, 12 * s, y, e.Item.Width - 12 * s, y);
    }

    protected override void OnRenderArrow(ToolStripArrowRenderEventArgs e)
    {
        e.ArrowColor = t.Dim;
        base.OnRenderArrow(e);
    }
}
