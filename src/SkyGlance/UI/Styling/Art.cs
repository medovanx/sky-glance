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
                        IconWeather = "\uE9CA", IconChevronDown = "\uE70D", IconCalendar = "\uE787", IconPrev = "\uE76B", IconNext = "\uE76C", IconPrevYear = "\uE892", IconNextYear = "\uE893", IconDelete = "\uE74D", IconAdd = "\uE710",
                        IconOpen = "\uE8A7", IconDocument = "\uE8A5",
                        IconInfo = "\uE946", IconGlobe = "\uE774", IconPower = "\uE7E8", IconBell = "\uEA8F";

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

    /// <summary>
    /// Draws an icon-font glyph centred on its ink, not its text box: some glyphs sit off-centre
    /// inside their box, so centring the box looks lopsided.
    /// </summary>
    public static void GlyphCentered(Graphics g, string glyph, float emPixels, RectangleF box, Color c)
    {
        using var path = new GraphicsPath();
        using var family = new FontFamily(IconFontName);
        path.AddString(glyph, family, (int)FontStyle.Regular, emPixels, PointF.Empty, StringFormat.GenericTypographic);
        var b = path.GetBounds();
        if (b.Width <= 0 || b.Height <= 0) return;
        using (var m = new Matrix())
        {
            m.Translate(box.Left + (box.Width - b.Width) / 2f - b.Left, box.Top + (box.Height - b.Height) / 2f - b.Top);
            path.Transform(m);
        }
        var old = g.SmoothingMode;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        using var brush = new SolidBrush(c);
        g.FillPath(brush, path);
        g.SmoothingMode = old;
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

    /// <param name="pulse">0 to 1 through one slow breath of the glow when animating, or negative for none.</param>
    static void SunDisc(Graphics g, PointF c, float r, bool rays, float pulse = -1)
    {
        if (pulse >= 0)
        {
            // A soft glow that swells and fades, behind the rays.
            float k = (1 - MathF.Cos(pulse * MathF.PI * 2)) / 2, gr = r * (1.4f + 0.6f * k);
            using var glow = new GraphicsPath();
            glow.AddEllipse(c.X - gr, c.Y - gr, gr * 2, gr * 2);
            using var gb = new PathGradientBrush(glow) { CenterColor = Color.FromArgb((int)(120 + 110 * k), Sun), SurroundColors = [Color.FromArgb(0, Sun)] };
            g.FillPath(gb, glow);
        }
        if (rays)
        {
            using var pen = new Pen(Sun, Math.Max(1f, r * 0.2f)) { StartCap = LineCap.Round, EndCap = LineCap.Round };
            for (int i = 0; i < 8; i++)
            {
                // While animating the rays turn one ray's width per cycle, so the loop is seamless.
                double a = Math.PI * i / 4 + (pulse >= 0 ? pulse * Math.PI / 4 : 0);
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
    /// <param name="drift">0 to 1 through the animation cycle (falling rain and snow, drifting clouds and fog, the sun's glow,
    /// twinkling stars, the lightning flash); negative for the still picture.</param>
    public static void Weather(Graphics g, RectangleF box, SkyIcon icon, bool day, float drift = -1)
    {
        float n = Math.Min(box.Width, box.Height);
        float x = box.Left + (box.Width - n) / 2, y = box.Top + (box.Height - n) / 2;
        var st = g.Save();
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.PixelOffsetMode = PixelOffsetMode.HighQuality;

        // Slow motions run once per animation cycle, so the last frame meets the first: sway is -1 to 1 and back.
        bool moving = drift >= 0;
        float sway = moving ? MathF.Sin(drift * MathF.PI * 2) : 0;
        void Body(PointF c, float r)
        {
            if (day) SunDisc(g, c, r, true, drift);
            else { MoonDisc(g, c, r); if (moving) Stars(g, c, r, drift); }
        }
        var top = new RectangleF(x + n * 0.06f, y + n * 0.1f, n * 0.88f, n * 0.56f);

        switch (icon)
        {
            case SkyIcon.Clear:
                Body(new PointF(x + n / 2, y + n / 2), day ? n * 0.24f : n * 0.34f);
                break;
            case SkyIcon.PartlyCloudy:
                Body(new PointF(x + n * 0.36f, y + n * 0.36f), day ? n * 0.17f : n * 0.24f);
                // The cloud drifts a little to and fro in front of the sun or moon.
                Cloud(g, new RectangleF(x + n * 0.2f + sway * n * 0.1f, y + n * 0.4f, n * 0.76f, n * 0.48f), CloudWhite, n);
                break;
            case SkyIcon.Cloudy:
                // The two clouds drift past each other.
                Cloud(g, new RectangleF(x + n * 0.3f + sway * n * 0.1f, y + n * 0.16f, n * 0.64f, n * 0.42f), CloudGrey, n);
                Cloud(g, new RectangleF(x + n * 0.04f - sway * n * 0.08f, y + n * 0.36f, n * 0.8f, n * 0.5f), CloudWhite, n);
                break;
            case SkyIcon.Fog:
                Cloud(g, top, CloudGrey, n);
                using (var pen = new Pen(Color.FromArgb(200, 210, 220, 232), n * 0.07f) { StartCap = LineCap.Round, EndCap = LineCap.Round })
                {
                    // The mist lines slide sideways, in opposite directions.
                    float m1 = sway * n * 0.12f, m2 = -sway * n * 0.1f;
                    g.DrawLine(pen, x + n * 0.14f + m1, y + n * 0.76f, x + n * 0.86f + m1, y + n * 0.76f);
                    g.DrawLine(pen, x + n * 0.26f + m2, y + n * 0.9f, x + n * 0.74f + m2, y + n * 0.9f);
                }
                break;
            case SkyIcon.Thunder:
                Cloud(g, top, StormCloud, n);
                PointF[] bolt = [new(x + n * 0.54f, y + n * 0.56f), new(x + n * 0.38f, y + n * 0.8f), new(x + n * 0.5f, y + n * 0.8f),
                                 new(x + n * 0.42f, y + n * 0.98f), new(x + n * 0.66f, y + n * 0.72f), new(x + n * 0.53f, y + n * 0.72f), new(x + n * 0.62f, y + n * 0.56f)];
                // Once a cycle the bolt flickers twice: brighter, with a glow around it.
                bool flash = moving && (drift < 0.04f || drift is >= 0.08f and < 0.11f);
                if (flash)
                {
                    using var halo = new Pen(Color.FromArgb(110, 255, 245, 190), n * 0.12f) { LineJoin = LineJoin.Round };
                    g.DrawPolygon(halo, bolt);
                }
                using (var b = new SolidBrush(flash ? Color.FromArgb(255, 252, 225) : Bolt)) g.FillPolygon(b, bolt);
                Drops(g, x, y, n, [0.24f, 0.8f], 0.16f, drift);
                break;
            default: // drizzle, rain, sleet, snow
                Cloud(g, top, icon == SkyIcon.Rain ? CloudGrey : CloudWhite, n);
                if (icon == SkyIcon.Snow) Flakes(g, x, y, n, [0.28f, 0.5f, 0.72f], drift);
                else if (icon == SkyIcon.Sleet) { Drops(g, x, y, n, [0.3f, 0.66f], 0.2f, drift); Flakes(g, x, y, n, [0.48f], drift); }
                else Drops(g, x, y, n, icon == SkyIcon.Drizzle ? [0.32f, 0.52f, 0.72f] : [0.26f, 0.44f, 0.62f, 0.8f], icon == SkyIcon.Drizzle ? 0.12f : 0.22f, drift);
                break;
        }
        g.Restore(st);
    }

    /// <summary>Three tiny stars around a clear night's moon, each twinkling at its own moment in the cycle.</summary>
    static void Stars(Graphics g, PointF c, float r, float drift)
    {
        (float dx, float dy, float size, float offset)[] stars = [(-1.25f, -0.85f, 0.16f, 0f), (1.15f, 0.95f, 0.12f, 0.33f), (-1.05f, 1.05f, 0.1f, 0.66f)];
        foreach (var (dx, dy, size, offset) in stars)
        {
            float k = (1 - MathF.Cos((drift + offset) * MathF.PI * 2)) / 2;
            float s = r * size * (0.7f + 0.5f * k), sx = c.X + dx * r, sy = c.Y + dy * r;
            using var b = new SolidBrush(Color.FromArgb((int)(60 + 195 * k), 255, 250, 225));
            // A four-pointed sparkle: two thin diamonds crossed.
            g.FillPolygon(b, new PointF[] { new(sx, sy - s * 2), new(sx + s * 0.45f, sy), new(sx, sy + s * 2), new(sx - s * 0.45f, sy) });
            g.FillPolygon(b, new PointF[] { new(sx - s * 2, sy), new(sx, sy + s * 0.45f), new(sx + s * 2, sy), new(sx, sy - s * 0.45f) });
        }
    }

    /// <summary>Where drop <paramref name="i"/> is in its fall (0 to 1) when animating, staggered so they don't move together.</summary>
    static float Fall(float drift, int i) => (drift + i * 0.37f) % 1f;

    static void Drops(Graphics g, float x, float y, float n, float[] xs, float len, float drift = -1)
    {
        float w = Math.Max(1.2f, n * 0.065f);
        for (int i = 0; i < xs.Length; i++)
        {
            float top = y + n * (i % 2 == 0 ? 0.72f : 0.76f), dx = 0;
            int alpha = 255;
            if (drift >= 0)
            {
                // Fall a short way along the drop's slant from under the cloud, fading out near the end.
                float f = Fall(drift, i), d = n * 0.16f * f;
                top = y + n * 0.66f + d;
                dx = -d * 0.27f;
                alpha = (int)(255 * Math.Min(1f, Math.Min(f * 5f, (1 - f) * 2.2f)));
            }
            using var pen = new Pen(Color.FromArgb(alpha, Drop), w) { StartCap = LineCap.Round, EndCap = LineCap.Round };
            g.DrawLine(pen, x + n * xs[i] + dx, top, x + n * (xs[i] - 0.06f) + dx, top + n * len);
        }
    }

    static void Flakes(Graphics g, float x, float y, float n, float[] xs, float drift = -1)
    {
        for (int i = 0; i < xs.Length; i++)
        {
            float cx = x + n * xs[i], cy = y + n * (i % 2 == 0 ? 0.82f : 0.88f), r = n * 0.08f;
            int alpha = 255;
            if (drift >= 0)
            {
                // Snow floats down slowly, swaying from side to side.
                float f = Fall(drift, i);
                cy = y + n * 0.74f + n * 0.2f * f;
                cx += (float)Math.Sin((f + i * 0.3f) * Math.PI * 2) * n * 0.03f;
                alpha = (int)(255 * Math.Min(1f, Math.Min(f * 5f, (1 - f) * 2.5f)));
            }
            using var pen = new Pen(Color.FromArgb(alpha, 235, 244, 255), Math.Max(1f, n * 0.04f)) { StartCap = LineCap.Round, EndCap = LineCap.Round };
            using var rim = new Pen(Color.FromArgb(alpha * 90 / 255, 70, 110, 160), Math.Max(1f, n * 0.07f)) { StartCap = LineCap.Round, EndCap = LineCap.Round };
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
    /// <param name="shift">Pixels to slide the pattern to the right, for a slow drift; it repeats, so any value works.</param>
    public static void CloudPattern(Graphics g, Rectangle area, float cell, Color c, float shift = 0)
    {
        var st = g.Save();
        g.SetClip(area);
        g.SmoothingMode = SmoothingMode.AntiAlias;
        using var brush = new SolidBrush(c);
        int row = 0;
        for (float yy = area.Top - cell * 0.3f; yy < area.Bottom; yy += cell * 0.62f, row++)
            for (float xx = area.Left - cell * 2.1f + (row % 2) * cell * 0.55f + shift % (cell * 1.1f); xx < area.Right; xx += cell * 1.1f)
            {
                using var p = CloudPath(new RectangleF(xx, yy, cell * 0.6f, cell * 0.32f));
                g.FillPath(brush, p);
            }
        g.Restore(st);
    }

    /// <summary>
    /// The weather behind a header, for the flyout's animation: rain streaks or snowflakes falling across it,
    /// or stars twinkling on a clear night. <paramref name="seconds"/> is the time since the animation started.
    /// Each particle has a fixed place from its index, so frames follow on smoothly without random jumps.
    /// </summary>
    public static void HeaderWeather(Graphics g, Rectangle area, SkyIcon icon, bool day, float seconds, float s)
    {
        var st = g.Save();
        g.SetClip(area);
        g.SmoothingMode = SmoothingMode.AntiAlias;
        static float Hash(int i, int k) { uint h = (uint)(i * 374761393 + k * 668265263); h = (h ^ (h >> 13)) * 1274126177; return (h ^ (h >> 16)) / (float)uint.MaxValue; }
        float w = area.Width, h = area.Height;
        if (icon is SkyIcon.Rain or SkyIcon.Drizzle or SkyIcon.Thunder or SkyIcon.Sleet)
        {
            int count = icon == SkyIcon.Drizzle ? 26 : 44;
            float len = (icon == SkyIcon.Drizzle ? 8 : 14) * s, speed = (icon == SkyIcon.Drizzle ? 120 : 260) * s;
            using var pen = new Pen(Color.FromArgb(icon == SkyIcon.Drizzle ? 50 : 70, 220, 235, 255), Math.Max(1f, 1.2f * s)) { StartCap = LineCap.Round, EndCap = LineCap.Round };
            for (int i = 0; i < count; i++)
            {
                float travel = h + len * 2, yy = (Hash(i, 1) * travel + seconds * speed * (0.8f + 0.4f * Hash(i, 2))) % travel - len;
                float xx = area.Left + Hash(i, 3) * (w + h * 0.25f) - yy * 0.25f;
                g.DrawLine(pen, xx, area.Top + yy, xx - len * 0.25f, area.Top + yy + len);
            }
        }
        if (icon is SkyIcon.Snow or SkyIcon.Sleet)
        {
            int count = icon == SkyIcon.Snow ? 34 : 14;
            for (int i = 0; i < count; i++)
            {
                float r = (1.2f + 1.6f * Hash(i, 4)) * s, travel = h + r * 4;
                float yy = (Hash(i, 5) * travel + seconds * (22 + 18 * Hash(i, 6)) * s) % travel - r * 2;
                float xx = area.Left + Hash(i, 7) * w + MathF.Sin(seconds * 1.3f + i) * 6 * s;
                using var b = new SolidBrush(Color.FromArgb(150, 255, 255, 255));
                g.FillEllipse(b, xx - r, area.Top + yy - r, r * 2, r * 2);
            }
        }
        if (!day && icon is SkyIcon.Clear or SkyIcon.PartlyCloudy)
        {
            for (int i = 0; i < 26; i++)
            {
                float k = (1 + MathF.Sin(seconds * (1.2f + Hash(i, 8)) + i * 2.1f)) / 2;
                float r = (0.8f + 1.2f * Hash(i, 9)) * s * (0.6f + 0.6f * k);
                using var b = new SolidBrush(Color.FromArgb((int)(40 + 180 * k), 255, 252, 230));
                g.FillEllipse(b, area.Left + Hash(i, 10) * w - r, area.Top + Hash(i, 11) * h * 0.9f - r, r * 2, r * 2);
            }
        }
        g.Restore(st);
    }

    /// <summary>True for pictures with something falling: their animation runs faster than the slow sway of the others.</summary>
    public static bool Falls(SkyIcon icon) => icon is SkyIcon.Drizzle or SkyIcon.Rain or SkyIcon.Sleet or SkyIcon.Snow or SkyIcon.Thunder;

    /// <summary>
    /// How far the animation moves per frame (12 a second): rain loops in about 3 s, thunder in about 5 s so its
    /// flash comes now and then, and the sway of clouds, fog, sun and stars in about 6 s.
    /// </summary>
    public static float DriftStep(SkyIcon icon) => icon == SkyIcon.Thunder ? 0.015f : Falls(icon) ? 0.03f : 0.014f;

    /// <summary>Colour of the air quality bands, from green (good) to purple (extremely poor).</summary>
    public static Color AirColor(AirCategory c) => c switch
    {
        AirCategory.Good => Color.FromArgb(80, 200, 120),
        AirCategory.Fair => Color.FromArgb(150, 205, 80),
        AirCategory.Moderate => Color.FromArgb(240, 200, 60),
        AirCategory.Poor => Color.FromArgb(245, 140, 50),
        AirCategory.VeryPoor => Color.FromArgb(230, 70, 70),
        _ => Color.FromArgb(160, 80, 200),
    };

    /// <summary>
    /// The moon as it looks tonight: the lit part of the disc for a fraction <paramref name="lit"/> (0 new, 1 full),
    /// on the right while it waxes and the left while it wanes (as seen from the northern hemisphere; flipped south of the equator).
    /// </summary>
    public static void MoonPicture(Graphics g, RectangleF box, double lit, bool litOnRight, Color dark)
    {
        var st = g.Save();
        g.SmoothingMode = SmoothingMode.AntiAlias;
        float n = Math.Min(box.Width, box.Height);
        var disc = new RectangleF(box.X + (box.Width - n) / 2, box.Y + (box.Height - n) / 2, n, n);
        using (var b = new SolidBrush(dark)) g.FillEllipse(b, disc);
        // The terminator is an ellipse whose half-width runs from n/2 (new or full) through 0 (quarter).
        float half = n / 2, cx = disc.X + half, k = (float)Math.Abs(1 - 2 * lit) * half;
        using var path = new GraphicsPath();
        // Outer edge: the half of the disc on the lit side.
        path.AddArc(disc, litOnRight ? -90 : 90, 180);
        // Inner edge: the terminator back to the top, bulging out (gibbous) or in (crescent).
        var term = new RectangleF(cx - k, disc.Y, Math.Max(0.01f, 2 * k), n);
        // From the bottom (lit on the right) or the top (lit on the left) back round: a gibbous moon's terminator
        // bulges into the dark half, a crescent's into the lit half.
        bool gibbous = lit > 0.5;
        path.AddArc(term, litOnRight ? 90 : -90, gibbous ? 180 : -180);
        path.CloseFigure();
        using (var b = new SolidBrush(Color.FromArgb(245, 240, 220))) g.FillPath(b, path);
        g.Restore(st);
    }

    /// <summary>The brand mark: a sun peeking out behind a cloud.</summary>
    public static void Mark(Graphics g, RectangleF box) => Weather(g, box, SkyIcon.PartlyCloudy, true);
}
