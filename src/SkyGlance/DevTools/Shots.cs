using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

namespace SkyGlance.DevTools;

/// <summary>
/// --shots &lt;dir&gt;: renders the widget, flyout and settings pages to PNGs for the README, from
/// <see cref="DemoData"/> only. Nothing goes online and everything is drawn off-screen (DrawToBitmap), so no other
/// window and none of the user's own data, such as their location, can end up in an image.
/// </summary>
static class Shots
{
    public static int Run(string dir)
    {
        Directory.CreateDirectory(dir);
        ApplicationConfiguration.Initialize();
        var all = DemoData.Forecasts();
        var forecast = all[Place.KeyOf(DemoData.Lat, DemoData.Lon)];
        foreach (var lang in new[] { "en", "ar" })
        {
            var app = new SkyGlanceApp(DemoData.Settings(lang), all, DemoData.World());
            var c = forecast.Current;

            WidgetStrip(Path.Combine(dir, $"widget-{lang}.png"), UnitFormat.Temperature(c.TemperatureC, TemperatureUnit.Celsius),
                L.Condition(c.Code), app.HighLow(), WeatherCode.Icon(c.Code), c.IsDay, lang == "ar");

            foreach (var dark in new[] { true, false })
            {
                var flyout = new WeatherFlyout(app);
                flyout.ShowForShot(dark);
                SaveControl(flyout, Path.Combine(dir, $"flyout-{lang}-{(dark ? "dark" : "light")}.png"), rounded: true);
                flyout.Dispose();
            }

            var settings = new SettingsForm(app) { StartPosition = FormStartPosition.Manual, Location = new Point(-4000, -4000) };
            settings.Show();
            for (int i = 0; i < settings.PageCount; i++)
            {
                settings.ShowPage(i);
                SaveClient(settings, Path.Combine(dir, $"settings-{i}-{lang}.png"));
            }
            settings.Close();

            var day = new DayForm(app) { StartPosition = FormStartPosition.Manual, Location = new Point(-4000, -4000) };
            day.Show();
            day.ShowDemo(DemoData.Day(), DemoData.City(lang));
            SaveClient(day, Path.Combine(dir, $"calendar-{lang}.png"));
            day.Close();
        }
        Icons(Path.Combine(dir, "icons.png"));
        return 0;
    }

    /// <summary>The widget on a dark taskbar-coloured strip, at 150% scale for a crisp README image.</summary>
    static void WidgetStrip(string path, string temp, string condition, string? highLow, SkyIcon icon, bool day, bool rtl)
    {
        const float s = 1.5f;
        int h = (int)(48 * s);
        using var widget = TaskbarWidget.Paint(temp, condition, highLow, icon, day, true, false, rtl, true, s, h);
        using var strip = new Bitmap(widget.Width + (int)(40 * s), h);
        using (var g = Graphics.FromImage(strip))
        {
            g.Clear(Color.FromArgb(28, 28, 28));
            g.DrawImage(widget, (int)(20 * s), 0);
        }
        strip.Save(path, ImageFormat.Png);
    }

    /// <summary>Every weather picture by day and by night on both backgrounds, for a visual check.</summary>
    static void Icons(string path)
    {
        var all = Enum.GetValues<SkyIcon>();
        int n = 56, w = all.Length * (n + 12) + 12;
        using var bmp = new Bitmap(w, 4 * (n + 12) + 12);
        using var g = Graphics.FromImage(bmp);
        g.Clear(Color.White);
        g.FillRectangle(new SolidBrush(Color.FromArgb(28, 31, 36)), 0, 2 * (n + 12) + 6, w, bmp.Height);
        for (int row = 0; row < 4; row++)
            for (int i = 0; i < all.Length; i++)
                Art.Weather(g, new RectangleF(12 + i * (n + 12), 12 + row * (n + 12), n, n), all[i], row % 2 == 0);
        bmp.Save(path, ImageFormat.Png);
    }

    /// <summary>A borderless popup drawn off-screen, with its rounded corners cut out.</summary>
    static void SaveControl(Control c, string path, bool rounded)
    {
        c.Refresh();
        Application.DoEvents();
        using var bmp = new Bitmap(c.Width, c.Height);
        c.DrawToBitmap(bmp, new Rectangle(Point.Empty, c.Size));
        if (!rounded) { bmp.Save(path, ImageFormat.Png); return; }
        using var outBmp = new Bitmap(c.Width, c.Height, PixelFormat.Format32bppArgb);
        using (var g = Graphics.FromImage(outBmp))
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            using var p = Art.RoundRect(new RectangleF(0, 0, c.Width - 1, c.Height - 1), 8 * c.DeviceDpi / 96f);
            using var tb = new TextureBrush(bmp);
            g.FillPath(tb, p);
        }
        outBmp.Save(path, ImageFormat.Png);
    }

    /// <summary>
    /// A window's client area (sidebar, page and buttons) without its title bar, drawn child by child off-screen.
    /// </summary>
    static void SaveClient(Form f, string path)
    {
        f.Refresh();
        for (int i = 0; i < 5; i++) Application.DoEvents();
        var size = f.ClientSize;
        using var bmp = new Bitmap(size.Width, size.Height);
        using (var g = Graphics.FromImage(bmp)) g.Clear(f.BackColor);
        foreach (Control c in f.Controls)
        {
            if (!c.Visible || c.Width <= 0 || c.Height <= 0) continue;
            using var part = new Bitmap(c.Width, c.Height);
            c.DrawToBitmap(part, new Rectangle(Point.Empty, c.Size));
            using var g = Graphics.FromImage(bmp);
            g.DrawImage(part, c.Location);
        }
        bmp.Save(path, ImageFormat.Png);
    }
}
