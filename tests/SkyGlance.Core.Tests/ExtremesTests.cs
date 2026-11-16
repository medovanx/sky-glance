using SkyGlance.Core.World;

namespace SkyGlance.Core.Tests;

public class ExtremesTests
{
    static readonly DateTime At = new(2026, 11, 16, 21, 15, 0, DateTimeKind.Utc);

    // Shape of a real two-spot answer (Death Valley, Vostok Station), trimmed.
    const string Two = """
    [{"latitude":36.46077,"longitude":-116.857796,"utc_offset_seconds":0,"current":{"time":"2026-11-16T21:15","temperature_2m":26.4,"weather_code":1,"is_day":1}},
     {"latitude":-78.558876,"longitude":106.70658,"location_id":1,"current":{"time":"2026-11-16T21:15","temperature_2m":-39.3,"weather_code":0,"is_day":1}}]
    """;

    [Fact]
    public void Reads_an_array_answer_in_request_order()
    {
        var r = Extremes.ParseReadings(Two);
        Assert.Equal(2, r.Count);
        Assert.Equal(0, r[0].Index);
        Assert.Equal(1, r[1].Index);
        Assert.Equal(-39.3, r[1].TemperatureC);
        Assert.True(r[0].IsDay);
    }

    [Fact]
    public void Picks_the_hottest_and_coldest()
    {
        var e = Extremes.Pick(Extremes.ParseReadings(Two), 2, At)!;
        Assert.Equal(0, e.Hottest.Index);
        Assert.Equal(1, e.Coldest.Index);
        Assert.Equal(2, e.Watched);
    }

    [Fact]
    public void A_single_spot_answer_is_one_object()
    {
        var r = Extremes.ParseReadings("""{"current":{"temperature_2m":12.5,"weather_code":3,"is_day":0}}""");
        Assert.Single(r);
        Assert.False(r[0].IsDay);
    }

    [Fact]
    public void Missing_values_are_skipped_and_empty_gives_null()
    {
        var r = Extremes.ParseReadings("""[{"current":{"temperature_2m":null}},{"current":{"temperature_2m":5}}]""");
        Assert.Single(r);
        Assert.Equal(1, r[0].Index);
        Assert.Null(Extremes.Pick([], 0, At));
    }

    [Fact]
    public void Url_lists_every_spot_in_order()
    {
        string url = Extremes.Url([(36.46, -116.87), (-78.46, 106.84)]);
        Assert.Contains("latitude=36.46,-78.46&longitude=-116.87,106.84", url);
    }
}
