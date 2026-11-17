namespace SkyGlance.Core.Weather;

/// <summary>A whole Open-Meteo answer: current conditions, the coming hours and the coming days.</summary>
public sealed record Forecast(
    double Latitude,
    double Longitude,
    string TimeZone,
    int UtcOffsetSeconds,
    DateTime FetchedUtc,
    CurrentConditions Current,
    IReadOnlyList<HourlyPoint> Hourly,
    IReadOnlyList<DailyPoint> Daily,
    IReadOnlyList<RainSlot>? Minutely = null)
{
    /// <summary>
    /// How much warmer (positive) or colder than the same hour yesterday it is now, in °C; null when the
    /// forecast has no hours from yesterday (a cache from before they were fetched).
    /// </summary>
    public double? ChangeSinceYesterday(DateTime localNow, double nowC)
    {
        var hour = new DateTime(localNow.Year, localNow.Month, localNow.Day, localNow.Hour, 0, 0).AddDays(-1);
        var then = Hourly.FirstOrDefault(h => h.Time == hour);
        return then == null ? null : nowC - then.TemperatureC;
    }

    /// <summary>The wall clock at the forecast location, which may be in another time zone than this PC.</summary>
    public DateTime LocalNow(DateTime utcNow) => DateTime.SpecifyKind(utcNow, DateTimeKind.Unspecified).AddSeconds(UtcOffsetSeconds);

    /// <summary>Up to <paramref name="count"/> hours starting with the hour that contains <paramref name="localNow"/>.</summary>
    public IReadOnlyList<HourlyPoint> HoursFrom(DateTime localNow, int count)
    {
        var start = new DateTime(localNow.Year, localNow.Month, localNow.Day, localNow.Hour, 0, 0);
        return Hourly.Where(h => h.Time >= start).Take(count).ToList();
    }

    /// <summary>The day containing <paramref name="localNow"/>, or the first day when the forecast is older than that.</summary>
    public DailyPoint? Today(DateTime localNow)
    {
        var d = DateOnly.FromDateTime(localNow);
        return Daily.FirstOrDefault(x => x.Date == d) ?? Daily.FirstOrDefault();
    }

    /// <summary>Days from today on.</summary>
    public IReadOnlyList<DailyPoint> DaysFrom(DateTime localNow) =>
        Daily.Where(x => x.Date >= DateOnly.FromDateTime(localNow)).ToList();

    /// <summary>True when the forecast is too old to show as current (the PC was offline for long).</summary>
    public bool IsStale(DateTime utcNow) => (utcNow - FetchedUtc).TotalHours >= 3;
}
