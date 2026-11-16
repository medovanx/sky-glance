using System.Drawing.Drawing2D;

namespace SkyGlance.UI.Styling;

/// <summary>Shared drawing: brand colours, the weather pictures, the cloud pattern and the brand mark.</summary>
static class Art
{
    public static readonly Color SkyLight = Color.FromArgb(64, 156, 236);
    public static readonly Color SkyDark = Color.FromArgb(20, 58, 132);
    public static readonly Color Sun = Color.FromArgb(255, 196, 46);
    public static readonly Color SunCore = Color.FromArgb(255, 226, 120);
    public static readonly Color Moon = Color.FromArgb(232, 236, 250);
    public static readonly Color CloudWhite = Color.FromArgb(246, 249, 253);
    public static readonly Color CloudGrey = Color.FromArgb(176, 188, 204);
    public static readonly Color StormCloud = Color.FromArgb(120, 132, 152);
    public static readonly Color Drop = Color.FromArgb(72, 170, 255);
    public static readonly Color Bolt = Color.FromArgb(255, 205, 60);

    // Windows icon font: Segoe Fluent Icons (Win11) or Segoe MDL2 Assets (Win10) share these code points.
    public const string IconSettings = "\uE713", IconRefresh = "\uE72C", IconLocation = "\uE81D", IconUpdate = "\uE895",
                        IconGeneral = "\uE771", IconSearch = "\uE721", IconTaskbar = "\uE7C4", IconRuler = "\uECC6",
                        IconWeather = "\uE9CA";

    public static readonly string IconFontName =
        new System.Drawing.Text.InstalledFontCollection().Families.Any(f => f.Name == "Segoe Fluent Icons")
            ? "Segoe Fluent Icons" : "Segoe MDL2 Assets";

    /// <summary>Header gradient for the sky right now: blue by day, deep indigo at night, slate when it is grey or stormy.</summary>
    public static (Color top, Color bottom) SkyColors(SkyIcon icon, bool day)
    {
        bool grey = icon is SkyIcon.Cloudy or SkyIcon.Fog or SkyIcon.Rain or SkyIcon.Drizzle or SkyIcon.Sleet or SkyIcon.Snow;
        if (icon == SkyIcon.Thunder) return (Color.FromArgb(70, 76, 112), Color.FromArgb(26, 28, 52));
        if (!day) return grey ? (Color.FromArgb(52, 62, 92), Color.FromArgb(18, 22, 42)) : (Color.FromArgb(40, 58, 128), Color.FromArgb(10, 16, 48));
        return grey ? (Color.FromArgb(104, 134, 172), Color.FromArgb(46, 66, 104)) : (SkyLight, SkyDark);
    }

    public static GraphicsPath RoundRect(RectangleF r, float rad)
    {
        var p = new GraphicsPath();
        float d = Math.Min(rad * 2, Math.Min(r.Width, r.Height));
        if (d <= 0) { p.AddRectangle(r); return p; }
        p.AddArc(r.Left, r.Top, d, d, 180, 90);
        p.AddArc(r.Right - d, r.Top, d, d, 270, 90);
        p.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
        p.AddArc(r.Left, r.Bottom - d, d, d, 90, 90);
        p.CloseFigure();
        return p;
    }

    public static void FillRound(Graphics g, RectangleF r, float rad, Color c)
    {
        using var p = RoundRect(r, rad);
        using var b = new SolidBrush(c);
        g.FillPath(b, p);
    }

    /// <summary>A puffy cloud filling <paramref name="r"/>: three bumps on a flat rounded base.</summary>
    public static GraphicsPath CloudPath(RectangleF r)
    {
        var p = new GraphicsPath { FillMode = FillMode.Winding };
        float w = r.Width, h = r.Height;
        p.AddEllipse(r.Left, r.Top + h * 0.34f, w * 0.42f, h * 0.66f);
        p.AddEllipse(r.Left + w * 0.2f, r.Top, w * 0.52f, h * 0.86f);
        p.AddEllipse(r.Left + w * 0.52f, r.Top + h * 0.24f, w * 0.48f, h * 0.76f);
        using (var b = RoundRect(new RectangleF(r.Left + w * 0.1f, r.Top + h * 0.5f, w * 0.82f, h * 0.5f), h * 0.25f)) p.AddPath(b, false);
        return p;
    }

    static void Cloud(Graphics g, RectangleF r, Color fill, float n)
    {
        using var p = CloudPath(r);
        // The rim is stroked first and the body filled over it, so only the outer edge of the bumps shows.
        // It keeps a white cloud visible on a light background.
        using (var rim = new Pen(Color.FromArgb(90, 60, 80, 110), Math.Max(1.5f, n * 0.05f)) { LineJoin = LineJoin.Round })
            g.DrawPath(rim, p);
        var shade = Color.FromArgb(fill.R * 86 / 100, fill.G * 88 / 100, Math.Min(255, fill.B * 92 / 100 + 8));
        using var b = new LinearGradientBrush(new RectangleF(r.Left, r.Top - 1, r.Width, r.Height + 2), fill, shade, LinearGradientMode.Vertical);
        g.FillPath(b, p);
    }

    static void SunDisc(Graphics g, PointF c, float r, bool rays)
    {
        if (rays)
        {
            using var pen = new Pen(Sun, Math.Max(1f, r * 0.2f)) { StartCap = LineCap.Round, EndCap = LineCap.Round };
            for (int i = 0; i < 8; i++)
            {
                double a = Math.PI * i / 4;
                float cos = (float)Math.Cos(a), sin = (float)Math.Sin(a);
                g.DrawLine(pen, c.X + cos * r * 1.35f, c.Y + sin * r * 1.35f, c.X + cos * r * 1.75f, c.Y + sin * r * 1.75f);
            }
        }
        var disc = new RectangleF(c.X - r, c.Y - r, r * 2, r * 2);
        using var path = new GraphicsPath();
        path.AddEllipse(disc);
        using var pb = new PathGradientBrush(path) { CenterColor = SunCore, SurroundColors = [Sun] };
        g.FillEllipse(pb, disc);
    }

    /// <summary>
    /// A crescent as one smooth path: the outer arc of the moon's disc and the inner arc of the "bite" circle,
    /// joined where the two circles cross. (A clipped Region would not be antialiased and looks jagged.)
    /// </summary>
    public static GraphicsPath CrescentPath(PointF c, float r)
    {
        var b = new PointF(c.X + r * 0.65f, c.Y - r * 0.25f); // centre of the bite, same radius
        float vx = b.X - c.X, vy = b.Y - c.Y, d = (float)Math.Sqrt(vx * vx + vy * vy);
        float h = (float)Math.Sqrt(r * r - d * d / 4);
        var m = new PointF(c.X + vx / 2, c.Y + vy / 2);
        var p1 = new PointF(m.X - vy / d * h, m.Y + vx / d * h);
        var p2 = new PointF(m.X + vy / d * h, m.Y - vx / d * h);
        static float Ang(PointF from, PointF to) => (float)(Math.Atan2(to.Y - from.Y, to.X - from.X) * 180 / Math.PI);
        // Sweep from a to b that passes through the direction "toward".
        static float Sweep(float a, float b, float toward)
        {
            float sw = ((b - a) % 360 + 360) % 360;
            float mid = a + sw / 2;
            float diff = Math.Abs(((mid - toward) % 360 + 540) % 360 - 180);
            return diff <= 90 ? sw : sw - 360;
        }
        float away = (float)(Math.Atan2(-vy, -vx) * 180 / Math.PI);
        var path = new GraphicsPath();
        float a1 = Ang(c, p1), a2 = Ang(c, p2);
        path.AddArc(c.X - r, c.Y - r, r * 2, r * 2, a1, Sweep(a1, a2, away));
        float b2 = Ang(b, p2), b1 = Ang(b, p1);
        path.AddArc(b.X - r, b.Y - r, r * 2, r * 2, b2, Sweep(b2, b1, away));
        path.CloseFigure();
        return path;
    }

    static void MoonDisc(Graphics g, PointF c, float r)
    {
        using var moon = CrescentPath(c, r);
        // The rim is stroked first and the body filled over it; it keeps the moon visible on a light background.
        using (var rim = new Pen(Color.FromArgb(90, 60, 80, 110), Math.Max(1.5f, r * 0.12f)) { LineJoin = LineJoin.Round })
            g.DrawPath(rim, moon);
        using var fill = new LinearGradientBrush(new RectangleF(c.X - r, c.Y - r - 1, r * 2, r * 2 + 2), Moon, Color.FromArgb(200, 208, 230), LinearGradientMode.ForwardDiagonal);
        g.FillPath(fill, moon);
    }

    /// <summary>Draws the picture for <paramref name="icon"/> centred in a square inside <paramref name="box"/>.</summary>
    public static void Weather(Graphics g, RectangleF box, SkyIcon icon, bool day)
    {
        float n = Math.Min(box.Width, box.Height);
        float x = box.Left + (box.Width - n) / 2, y = box.Top + (box.Height - n) / 2;
        var st = g.Save();
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.PixelOffsetMode = PixelOffsetMode.HighQuality;

        void Body(PointF c, float r) { if (day) SunDisc(g, c, r, true); else MoonDisc(g, c, r); }
        var top = new RectangleF(x + n * 0.06f, y + n * 0.1f, n * 0.88f, n * 0.56f);

        switch (icon)
        {
            case SkyIcon.Clear:
                Body(new PointF(x + n / 2, y + n / 2), day ? n * 0.24f : n * 0.34f);
                break;
            case SkyIcon.PartlyCloudy:
                Body(new PointF(x + n * 0.36f, y + n * 0.36f), day ? n * 0.17f : n * 0.24f);
                Cloud(g, new RectangleF(x + n * 0.2f, y + n * 0.4f, n * 0.76f, n * 0.48f), CloudWhite, n);
                break;
            case SkyIcon.Cloudy:
                Cloud(g, new RectangleF(x + n * 0.3f, y + n * 0.16f, n * 0.64f, n * 0.42f), CloudGrey, n);
                Cloud(g, new RectangleF(x + n * 0.04f, y + n * 0.36f, n * 0.8f, n * 0.5f), CloudWhite, n);
                break;
            case SkyIcon.Fog:
                Cloud(g, top, CloudGrey, n);
                using (var pen = new Pen(Color.FromArgb(200, 210, 220, 232), n * 0.07f) { StartCap = LineCap.Round, EndCap = LineCap.Round })
                {
                    g.DrawLine(pen, x + n * 0.14f, y + n * 0.76f, x + n * 0.86f, y + n * 0.76f);
                    g.DrawLine(pen, x + n * 0.26f, y + n * 0.9f, x + n * 0.74f, y + n * 0.9f);
                }
                break;
            case SkyIcon.Thunder:
                Cloud(g, top, StormCloud, n);
                PointF[] bolt = [new(x + n * 0.54f, y + n * 0.56f), new(x + n * 0.38f, y + n * 0.8f), new(x + n * 0.5f, y + n * 0.8f),
                                 new(x + n * 0.42f, y + n * 0.98f), new(x + n * 0.66f, y + n * 0.72f), new(x + n * 0.53f, y + n * 0.72f), new(x + n * 0.62f, y + n * 0.56f)];
                using (var b = new SolidBrush(Bolt)) g.FillPolygon(b, bolt);
                Drops(g, x, y, n, [0.24f, 0.8f], 0.16f);
                break;
            default: // drizzle, rain, sleet, snow
                Cloud(g, top, icon == SkyIcon.Rain ? CloudGrey : CloudWhite, n);
                if (icon == SkyIcon.Snow) Flakes(g, x, y, n, [0.28f, 0.5f, 0.72f]);
                else if (icon == SkyIcon.Sleet) { Drops(g, x, y, n, [0.3f, 0.66f], 0.2f); Flakes(g, x, y, n, [0.48f]); }
                else Drops(g, x, y, n, icon == SkyIcon.Drizzle ? [0.32f, 0.52f, 0.72f] : [0.26f, 0.44f, 0.62f, 0.8f], icon == SkyIcon.Drizzle ? 0.12f : 0.22f);
                break;
        }
        g.Restore(st);
    }

    static void Drops(Graphics g, float x, float y, float n, float[] xs, float len)
    {
        using var pen = new Pen(Drop, Math.Max(1.2f, n * 0.065f)) { StartCap = LineCap.Round, EndCap = LineCap.Round };
        for (int i = 0; i < xs.Length; i++)
        {
            float top = y + n * (i % 2 == 0 ? 0.72f : 0.76f);
            g.DrawLine(pen, x + n * xs[i], top, x + n * (xs[i] - 0.06f), top + n * len);
        }
    }

    static void Flakes(Graphics g, float x, float y, float n, float[] xs)
    {
        using var pen = new Pen(Color.FromArgb(235, 244, 255), Math.Max(1f, n * 0.04f)) { StartCap = LineCap.Round, EndCap = LineCap.Round };
        using var rim = new Pen(Color.FromArgb(90, 70, 110, 160), Math.Max(1f, n * 0.07f)) { StartCap = LineCap.Round, EndCap = LineCap.Round };
        for (int i = 0; i < xs.Length; i++)
        {
            float cx = x + n * xs[i], cy = y + n * (i % 2 == 0 ? 0.82f : 0.88f), r = n * 0.08f;
            foreach (var p in new[] { rim, pen })
                for (int k = 0; k < 3; k++)
                {
                    double a = Math.PI * k / 3;
                    float dx = (float)Math.Cos(a) * r, dy = (float)Math.Sin(a) * r;
                    g.DrawLine(p, cx - dx, cy - dy, cx + dx, cy + dy);
                }
        }
    }

    /// <summary>Faint little filled clouds in staggered rows, as a background pattern for headers.</summary>
    public static void CloudPattern(Graphics g, Rectangle area, float cell, Color c)
    {
        var st = g.Save();
        g.SetClip(area);
        g.SmoothingMode = SmoothingMode.AntiAlias;
        using var brush = new SolidBrush(c);
        int row = 0;
        for (float yy = area.Top - cell * 0.3f; yy < area.Bottom; yy += cell * 0.62f, row++)
            for (float xx = area.Left - cell + (row % 2) * cell * 0.55f; xx < area.Right; xx += cell * 1.1f)
            {
                using var p = CloudPath(new RectangleF(xx, yy, cell * 0.6f, cell * 0.32f));
                g.FillPath(brush, p);
            }
        g.Restore(st);
    }

    /// <summary>The brand mark: a sun peeking out behind a cloud.</summary>
    public static void Mark(Graphics g, RectangleF box) => Weather(g, box, SkyIcon.PartlyCloudy, true);
}
