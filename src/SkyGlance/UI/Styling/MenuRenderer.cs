using System.Drawing.Drawing2D;

namespace SkyGlance.UI.Styling;

/// <summary>
/// Draws menus (the city list, the right-click menu) in the app theme: a surface-coloured box with a thin border,
/// a rounded highlight under the pointer and an accent check mark.
/// </summary>
sealed class MenuRenderer(Theme t) : ToolStripRenderer
{
    /// <summary>Themes <paramref name="menu"/> and rounds its corners; call before each Show (the theme can change).</summary>
    public static void Apply(ToolStripDropDown menu, Theme t)
    {
        menu.Renderer = new MenuRenderer(t);
        menu.BackColor = t.Surface;
        menu.ForeColor = t.Text;
        menu.Font = new Font("Segoe UI", 9.5f);
        menu.Padding = new Padding(4);
        foreach (ToolStripItem item in menu.Items)
        {
            item.ForeColor = t.Text;
            if (item is ToolStripMenuItem { HasDropDownItems: true } sub) Apply(sub.DropDown, t);
        }
        menu.Opened -= Round;
        menu.Opened += Round;
        void Round(object? s, EventArgs e) { if (menu.IsHandleCreated) Native.RoundCorners(menu.Handle, t.Dark); }
    }

    protected override void OnRenderToolStripBackground(ToolStripRenderEventArgs e)
    {
        using var b = new SolidBrush(t.Surface);
        e.Graphics.FillRectangle(b, e.AffectedBounds);
    }

    protected override void OnRenderToolStripBorder(ToolStripRenderEventArgs e)
    {
        using var pen = new Pen(t.Border);
        var r = e.ToolStrip.ClientRectangle;
        e.Graphics.DrawRectangle(pen, r.X, r.Y, r.Width - 1, r.Height - 1);
    }

    protected override void OnRenderImageMargin(ToolStripRenderEventArgs e) { /* no grey icon column */ }

    protected override void OnRenderMenuItemBackground(ToolStripItemRenderEventArgs e)
    {
        if (!e.Item.Selected || !e.Item.Enabled) return;
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        var r = new Rectangle(2, 1, e.Item.Width - 4, e.Item.Height - 2);
        Art.FillRound(g, r, 4, Color.FromArgb(t.Dark ? 44 : 30, t.Accent));
    }

    protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
    {
        e.TextColor = e.Item.Enabled ? t.Text : t.Dim;
        if (e.Item is ToolStripMenuItem { Checked: true }) e.TextFont = new Font(e.TextFont ?? e.Item.Font, FontStyle.Bold);
        base.OnRenderItemText(e);
    }

    protected override void OnRenderItemCheck(ToolStripItemImageRenderEventArgs e)
    {
        using var f = new Font(Art.IconFontName, 9f);
        TextRenderer.DrawText(e.Graphics, "\uE73E", f, e.ImageRectangle, t.Accent, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
    }

    protected override void OnRenderArrow(ToolStripArrowRenderEventArgs e)
    {
        e.ArrowColor = t.Dim;
        base.OnRenderArrow(e);
    }

    protected override void OnRenderSeparator(ToolStripSeparatorRenderEventArgs e)
    {
        using var pen = new Pen(t.Border);
        int y = e.Item.Height / 2;
        e.Graphics.DrawLine(pen, 8, y, e.Item.Width - 8, y);
    }
}
