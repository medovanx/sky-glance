using System.Drawing.Drawing2D;

namespace SkyGlance.UI.Styling;

static class Styler
{
    /// <summary>Applies the theme to standard controls (inputs, labels, buttons) inside a container.</summary>
    public static void Apply(Control root, Theme t)
    {
        foreach (Control c in root.Controls)
        {
            switch (c)
            {
                case ComboBox cb:
                    ThemeCombo(cb, t); break;
                case TextBox tb:
                    tb.BorderStyle = BorderStyle.FixedSingle; tb.BackColor = t.Input; tb.ForeColor = t.Text; break;
                case NumericUpDown nu:
                    nu.BorderStyle = BorderStyle.FixedSingle; nu.BackColor = t.Input; nu.ForeColor = t.Text; break;
                case Button b:
                    b.FlatStyle = FlatStyle.Flat;
                    b.FlatAppearance.BorderColor = t.Border;
                    b.BackColor = t.Input; b.ForeColor = t.Text;
                    b.FlatAppearance.MouseOverBackColor = t.Dark ? Color.FromArgb(64, 68, 68) : Color.FromArgb(236, 240, 239);
                    b.Cursor = Cursors.Hand;
                    ThemeDisabled(b, t);
                    break;
                case Label l when l.Tag as string == "keep":
                    l.BackColor = Color.Transparent; break;
                case Label l:
                    l.ForeColor = l.Tag as string == "dim" ? t.Dim : t.Text; l.BackColor = Color.Transparent; break;
                case ToggleSwitch ts: ts.Theme = t; break;
                case ThemedCombo tc: tc.Theme = t; break;
                case ThemedNumber tn: tn.Theme = t; break;
                case ThemedText tt: tt.Theme = t; break;
                case Segmented sg: sg.Theme = t; break;
                case Section se: se.Theme = t; break;
            }
            if (c is Panel or TableLayoutPanel or FlowLayoutPanel && c is not Section) c.BackColor = Color.Transparent;
            if (c is not (ThemedText or ThemedNumber or ThemedCombo)) Apply(c, t); // they style their own insides
        }
    }

    /// <summary>First real (non-transparent) background colour up the parent chain.</summary>
    public static Color Backdrop(Control c, Color fallback)
    {
        for (var p = c.Parent; p != null; p = p.Parent)
            if (p is Section s) return s.Theme.Surface;
            else if (p.BackColor.A == 255) return p.BackColor;
        return fallback;
    }

    /// <summary>Dark-friendly dropdown: owner-drawn items and closed box in the theme colours.</summary>
    static void ThemeCombo(ComboBox cb, Theme t)
    {
        cb.FlatStyle = FlatStyle.Flat;
        cb.BackColor = t.Input;
        cb.ForeColor = t.Text;
        cb.DrawMode = DrawMode.OwnerDrawFixed;
        cb.ItemHeight = 22;
        cb.DrawItem += (_, e) =>
        {
            if (e.Index < 0) return;
            bool sel = (e.State & DrawItemState.Selected) != 0 && (e.State & DrawItemState.ComboBoxEdit) == 0;
            using var b = new SolidBrush(sel ? t.AccentFill : t.Input);
            e.Graphics.FillRectangle(b, e.Bounds);
            var flags = TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis
                        | (cb.RightToLeft == RightToLeft.Yes ? TextFormatFlags.Right | TextFormatFlags.RightToLeft : TextFormatFlags.Left);
            var r = Rectangle.Inflate(e.Bounds, -4, 0);
            TextRenderer.DrawText(e.Graphics, cb.Items[e.Index]?.ToString(), cb.Font, r, sel ? Color.White : t.Text, flags);
        };
    }

    /// <summary>
    /// A disabled flat button draws its text in the system's dark grey, which vanishes on a dark theme.
    /// Repaint the text in the theme's dim colour instead.
    /// </summary>
    static void ThemeDisabled(Button b, Theme t)
    {
        b.Paint += (_, e) =>
        {
            if (b.Enabled) return;
            var inner = Rectangle.Inflate(b.ClientRectangle, -1, -1);
            using (var bg = new SolidBrush(b.BackColor)) e.Graphics.FillRectangle(bg, inner);
            TextRenderer.DrawText(e.Graphics, b.Text, b.Font, b.ClientRectangle, t.Dim,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | (b.RightToLeft == RightToLeft.Yes ? TextFormatFlags.RightToLeft : 0));
        };
    }

    public static void Primary(Button b, Theme t)
    {
        b.FlatStyle = FlatStyle.Flat;
        b.BackColor = t.AccentFill;
        b.ForeColor = Color.White;
        b.FlatAppearance.BorderColor = t.AccentFill;
        b.FlatAppearance.MouseOverBackColor = t.AccentHover;
        b.Font = new Font("Segoe UI Semibold", 9.5f);
    }
}
