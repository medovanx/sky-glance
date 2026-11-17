using System.Globalization;
using System.Text.Json;

namespace SkyGlance.Core.Weather;

/// <summary>
/// Builds the Open-Meteo forecast request and turns the JSON answer into a <see cref="Forecast"/>.
/// Everything is requested in metric units; the app converts for display, so a unit change needs no new request.
/// </summary>
public static class OpenMeteo
{
    public const string Host = "https://api.open-meteo.com/v1/forecast";

    const string CurrentFields = "temperature_2m,apparent_temperature,relative_humidity_2m,is_day,weather_code,wind_speed_10m,wind_direction_10m,wind_gusts_10m,precipitation,pressure_msl,cloud_cover";
    const string HourlyFields = "temperature_2m,weather_code,precipitation_probability,is_day";
    const string DailyFields = "weather_code,temperature_2m_max,temperature_2m_min,precipitation_probability_max,sunrise,sunset,uv_index_max,wind_speed_10m_max";

    public static string Url(double latitude, double longitude, int days = 7) => string.Create(CultureInfo.InvariantCulture,
        $"{Host}?latitude={latitude:0.####}&longitude={longitude:0.####}&current={CurrentFields}&hourly={HourlyFields}&daily={DailyFields}&minutely_15=precipitation&timezone=auto&forecast_days={days}&forecast_hours=48&past_hours=24&forecast_minutely_15=12&past_minutely_15=4");

    public static Forecast Parse(string json, DateTime fetchedUtc)
    {
        using var doc = JsonDocument.Parse(json);
        var r = doc.RootElement;
        if (r.TryGetProperty("error", out var err) && err.ValueKind == JsonValueKind.True)
            throw new FormatException(Str(r, "reason") ?? "Open-Meteo returned an error");

        var c = r.GetProperty("current");
        var current = new CurrentConditions(
            Time(c.GetProperty("time")),
            Num(c, "temperature_2m"),
            Num(c, "apparent_temperature"),
            (int)Math.Round(Num(c, "relative_humidity_2m")),
            Num(c, "is_day") >= 1,
            (int)Num(c, "weather_code"),
            Num(c, "wind_speed_10m"),
            (int)Math.Round(Num(c, "wind_direction_10m")),
            Num(c, "wind_gusts_10m"),
            Num(c, "precipitation"),
            Num(c, "pressure_msl"),
            (int)Math.Round(Num(c, "cloud_cover")));

        var hourly = new List<HourlyPoint>();
        if (r.TryGetProperty("hourly", out var h))
        {
            var times = h.GetProperty("time");
            for (int i = 0; i < times.GetArrayLength(); i++)
            {
                double t = At(h, "temperature_2m", i);
                if (double.IsNaN(t)) continue;
                hourly.Add(new HourlyPoint(Time(times[i]), t, (int)Zero(At(h, "weather_code", i)),
                    (int)Math.Round(Zero(At(h, "precipitation_probability", i))), Zero(At(h, "is_day", i)) >= 1));
            }
        }

        var daily = new List<DailyPoint>();
        if (r.TryGetProperty("daily", out var d))
        {
            var times = d.GetProperty("time");
            for (int i = 0; i < times.GetArrayLength(); i++)
            {
                double max = At(d, "temperature_2m_max", i), min = At(d, "temperature_2m_min", i);
                if (double.IsNaN(max) || double.IsNaN(min)) continue;
                daily.Add(new DailyPoint(
                    DateOnly.ParseExact(times[i].GetString()!, "yyyy-MM-dd", CultureInfo.InvariantCulture),
                    (int)Zero(At(d, "weather_code", i)), max, min,
                    (int)Math.Round(Zero(At(d, "precipitation_probability_max", i))),
                    TimeOrNull(d, "sunrise", i), TimeOrNull(d, "sunset", i),
                    Zero(At(d, "uv_index_max", i)), Zero(At(d, "wind_speed_10m_max", i))));
            }
        }

        // Rain in 15-minute steps for the next three hours (native in Europe and North America, from the hourly model elsewhere).
        List<RainSlot>? minutely = null;
        if (r.TryGetProperty("minutely_15", out var q) && q.TryGetProperty("time", out var qt))
        {
            minutely = [];
            for (int i = 0; i < qt.GetArrayLength(); i++)
            {
                double mm = At(q, "precipitation", i);
                if (!double.IsNaN(mm)) minutely.Add(new RainSlot(Time(qt[i]), mm));
            }
        }

        return new Forecast(Num(r, "latitude"), Num(r, "longitude"), Str(r, "timezone") ?? "GMT",
            (int)Num(r, "utc_offset_seconds"), fetchedUtc, current, hourly, daily, minutely);
    }

    static double Zero(double v) => double.IsNaN(v) ? 0 : v;

    static double Num(JsonElement e, string name) =>
        e.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.Number ? v.GetDouble() : 0;

    /// <summary>Value <paramref name="i"/> of an array field; NaN when the field is missing or the value is null.</summary>
    static double At(JsonElement e, string name, int i) =>
        e.TryGetProperty(name, out var a) && a.ValueKind == JsonValueKind.Array && i < a.GetArrayLength() && a[i].ValueKind == JsonValueKind.Number
            ? a[i].GetDouble() : double.NaN;

    static string? Str(JsonElement e, string name) =>
        e.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() : null;

    static DateTime Time(JsonElement e) =>
        DateTime.ParseExact(e.GetString()!, ["yyyy-MM-dd'T'HH:mm", "yyyy-MM-dd'T'HH:mm:ss"], CultureInfo.InvariantCulture, DateTimeStyles.None);

    static DateTime? TimeOrNull(JsonElement e, string name, int i)
    {
        if (!e.TryGetProperty(name, out var a) || a.ValueKind != JsonValueKind.Array || i >= a.GetArrayLength()) return null;
        var v = a[i];
        if (v.ValueKind != JsonValueKind.String) return null;
        // Polar day or night: Open-Meteo sends midnight for a sun that never rises or sets.
        var t = Time(v);
        return t.TimeOfDay == TimeSpan.Zero ? null : t;
    }
}
