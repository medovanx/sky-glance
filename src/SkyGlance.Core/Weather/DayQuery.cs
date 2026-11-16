using System.Globalization;
using System.Text.Json;

namespace SkyGlance.Core.Weather;

/// <summary>Asks Open-Meteo for the weather of a single calendar day, from the forecast or from the archive.</summary>
public static class DayQuery
{
    public const string ArchiveHost = "https://archive-api.open-meteo.com/v1/archive";
    public static readonly DateOnly FirstDay = new(1940, 1, 1);
    /// <summary>Days back that the forecast API still covers (it allows a little more; this keeps clear of the edge).</summary>
    public const int ForecastPastDays = 90;
    public const int ForecastAheadDays = 15;

    public static DateOnly LastDay(DateOnly today) => today.AddDays(ForecastAheadDays);

    public static DaySource Source(DateOnly date, DateOnly today) =>
        date < FirstDay || date > LastDay(today) ? DaySource.OutOfRange
        : date >= today.AddDays(-ForecastPastDays) ? DaySource.Forecast
        : DaySource.Archive;

    public static string Url(double latitude, double longitude, DateOnly date, DaySource source)
    {
        string daily = source == DaySource.Forecast
            ? "weather_code,temperature_2m_max,temperature_2m_min,precipitation_sum,precipitation_probability_max,sunrise,sunset,wind_speed_10m_max,uv_index_max"
            : "weather_code,temperature_2m_max,temperature_2m_min,precipitation_sum,sunrise,sunset,wind_speed_10m_max";
        string host = source == DaySource.Forecast ? OpenMeteo.Host : ArchiveHost;
        string d = date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        return string.Create(CultureInfo.InvariantCulture,
            $"{host}?latitude={latitude:0.####}&longitude={longitude:0.####}&start_date={d}&end_date={d}&daily={daily}&hourly=temperature_2m,weather_code,is_day&timezone=auto");
    }

    public static DayReport Parse(string json, DaySource source)
    {
        using var doc = JsonDocument.Parse(json);
        var r = doc.RootElement;
        if (r.TryGetProperty("error", out var err) && err.ValueKind == JsonValueKind.True)
            throw new FormatException(r.TryGetProperty("reason", out var why) ? why.GetString() : "Open-Meteo returned an error");
        var d = r.GetProperty("daily");
        double? Num(string name) =>
            d.TryGetProperty(name, out var a) && a.ValueKind == JsonValueKind.Array && a.GetArrayLength() > 0 && a[0].ValueKind == JsonValueKind.Number
                ? a[0].GetDouble() : null;
        DateTime? Time(string name)
        {
            if (!d.TryGetProperty(name, out var a) || a.ValueKind != JsonValueKind.Array || a.GetArrayLength() == 0 || a[0].ValueKind != JsonValueKind.String) return null;
            var t = DateTime.ParseExact(a[0].GetString()!, "yyyy-MM-dd'T'HH:mm", CultureInfo.InvariantCulture);
            return t.TimeOfDay == TimeSpan.Zero ? null : t; // polar day or night
        }
        double? max = Num("temperature_2m_max"), min = Num("temperature_2m_min");
        if (max == null || min == null) throw new FormatException("No data for this day");

        var hours = new List<HourlyPoint>();
        if (r.TryGetProperty("hourly", out var h) && h.TryGetProperty("time", out var times))
            for (int i = 0; i < times.GetArrayLength(); i++)
            {
                if (!h.TryGetProperty("temperature_2m", out var ta) || ta[i].ValueKind != JsonValueKind.Number) continue;
                int code = h.TryGetProperty("weather_code", out var ca) && ca[i].ValueKind == JsonValueKind.Number ? ca[i].GetInt32() : 0;
                bool day = h.TryGetProperty("is_day", out var da) && da[i].ValueKind == JsonValueKind.Number && da[i].GetInt32() == 1;
                hours.Add(new HourlyPoint(DateTime.ParseExact(times[i].GetString()!, "yyyy-MM-dd'T'HH:mm", CultureInfo.InvariantCulture),
                    ta[i].GetDouble(), code, 0, day));
            }

        var date = DateOnly.ParseExact(d.GetProperty("time")[0].GetString()!, "yyyy-MM-dd", CultureInfo.InvariantCulture);
        double? chance = Num("precipitation_probability_max");
        return new DayReport(date, source, (int)(Num("weather_code") ?? 0), max.Value, min.Value, Num("precipitation_sum") ?? 0,
            chance is { } c ? (int)Math.Round(c) : null, Num("wind_speed_10m_max") ?? 0, Num("uv_index_max"),
            Time("sunrise"), Time("sunset"), hours);
    }
}
