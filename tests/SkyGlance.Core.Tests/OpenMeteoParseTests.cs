using SkyGlance.Core.Weather;

namespace SkyGlance.Core.Tests;

public class OpenMeteoParseTests
{
    static readonly DateTime Fetched = new(2026, 11, 16, 20, 45, 0, DateTimeKind.Utc);

    static Forecast Berlin() => OpenMeteo.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures", "berlin.json")), Fetched);

    [Fact]
    public void Reads_current_conditions()
    {
        var f = Berlin();
        Assert.Equal(new DateTime(2026, 11, 16, 21, 45, 0), f.Current.Time);
        Assert.Equal(8.4, f.Current.TemperatureC);
        Assert.Equal(4.7, f.Current.FeelsLikeC);
        Assert.Equal(74, f.Current.Humidity);
        Assert.False(f.Current.IsDay);
        Assert.Equal(3, f.Current.Code);
        Assert.Equal(249, f.Current.WindDirection);
        Assert.Equal("Europe/Berlin", f.TimeZone);
        Assert.Equal(3600, f.UtcOffsetSeconds);
    }

    [Fact]
    public void Reads_hours_and_days()
    {
        var f = Berlin();
        Assert.Equal(48, f.Hourly.Count);
        Assert.Equal(new DateTime(2026, 11, 16, 21, 0, 0), f.Hourly[0].Time);
        Assert.Equal(8.5, f.Hourly[0].TemperatureC);
        Assert.Equal(43, f.Hourly[0].PrecipitationChance);
        Assert.Equal(7, f.Daily.Count);
        var today = f.Daily[0];
        Assert.Equal(new DateOnly(2026, 11, 16), today.Date);
        Assert.Equal(11.2, today.MaxC);
        Assert.Equal(7.2, today.MinC);
        Assert.Equal(80, today.Code);
        Assert.Equal(new DateTime(2026, 11, 16, 7, 29, 0), today.Sunrise);
        Assert.Equal(new DateTime(2026, 11, 16, 16, 11, 0), today.Sunset);
    }

    [Fact]
    public void Local_clock_uses_the_location_offset_not_the_pc()
    {
        var f = Berlin();
        Assert.Equal(new DateTime(2026, 11, 16, 21, 45, 0), f.LocalNow(Fetched));
        var hours = f.HoursFrom(f.LocalNow(Fetched), 24);
        Assert.Equal(24, hours.Count);
        Assert.Equal(21, hours[0].Time.Hour);
        Assert.Equal(new DateOnly(2026, 11, 16), f.Today(f.LocalNow(Fetched))!.Date);
    }

    [Fact]
    public void Today_after_midnight_moves_to_the_next_day()
    {
        var f = Berlin();
        var later = f.LocalNow(Fetched.AddHours(3));
        Assert.Equal(new DateOnly(2026, 11, 17), f.Today(later)!.Date);
        Assert.Equal(6, f.DaysFrom(later).Count);
    }

    [Fact]
    public void Stale_after_three_hours()
    {
        var f = Berlin();
        Assert.False(f.IsStale(Fetched.AddHours(2)));
        Assert.True(f.IsStale(Fetched.AddHours(3)));
    }

    [Fact]
    public void Nulls_and_polar_sun_are_tolerated()
    {
        const string json = """
        {"latitude":78.2,"longitude":15.6,"utc_offset_seconds":3600,"timezone":"Arctic/Longyearbyen",
         "current":{"time":"2026-12-01T12:00","temperature_2m":-12.5,"apparent_temperature":-19.1,"relative_humidity_2m":80,"is_day":0,"weather_code":71,"wind_speed_10m":20,"wind_direction_10m":90,"wind_gusts_10m":30,"precipitation":0.1,"pressure_msl":1010,"cloud_cover":90},
         "hourly":{"time":["2026-12-01T12:00","2026-12-01T13:00"],"temperature_2m":[-12.5,null],"weather_code":[71,null],"precipitation_probability":[null,10],"is_day":[0,0]},
         "daily":{"time":["2026-12-01"],"weather_code":[71],"temperature_2m_max":[-10.0],"temperature_2m_min":[-15.0],"precipitation_probability_max":[null],"sunrise":["2026-12-01T00:00"],"sunset":["2026-12-01T00:00"],"uv_index_max":[null],"wind_speed_10m_max":[25]}}
        """;
        var f = OpenMeteo.Parse(json, Fetched);
        Assert.Single(f.Hourly);
        Assert.Equal(0, f.Hourly[0].PrecipitationChance);
        Assert.Null(f.Daily[0].Sunrise);
        Assert.Null(f.Daily[0].Sunset);
        Assert.Equal(-12.5, f.Current.TemperatureC);
    }

    [Fact]
    public void Error_answer_throws()
    {
        var ex = Assert.Throws<FormatException>(() => OpenMeteo.Parse("""{"error":true,"reason":"Latitude must be in range of -90 to 90"}""", Fetched));
        Assert.Contains("Latitude", ex.Message);
    }

    [Fact]
    public void Url_uses_invariant_numbers()
    {
        var old = System.Globalization.CultureInfo.CurrentCulture;
        try
        {
            System.Globalization.CultureInfo.CurrentCulture = System.Globalization.CultureInfo.GetCultureInfo("de-DE");
            string url = OpenMeteo.Url(52.52, -13.4123456);
            Assert.Contains("latitude=52.52&longitude=-13.4123", url);
            Assert.Contains("timezone=auto", url);
        }
        finally { System.Globalization.CultureInfo.CurrentCulture = old; }
    }
}
