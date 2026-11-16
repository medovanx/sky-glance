using SkyGlance.Core.Weather;

namespace SkyGlance.Core.Tests;

public class DayQueryTests
{
    static readonly DateOnly Today = new(2026, 11, 16);

    [Theory]
    [InlineData(2026, 11, 16, DaySource.Forecast)]
    [InlineData(2026, 12, 1, DaySource.Forecast)]
    [InlineData(2026, 12, 2, DaySource.OutOfRange)]
    [InlineData(2026, 8, 18, DaySource.Forecast)]
    [InlineData(2026, 8, 17, DaySource.Archive)]
    [InlineData(1940, 1, 1, DaySource.Archive)]
    [InlineData(1939, 12, 31, DaySource.OutOfRange)]
    public void Picks_the_right_service_for_a_date(int y, int m, int d, DaySource expected) =>
        Assert.Equal(expected, DayQuery.Source(new DateOnly(y, m, d), Today));

    [Fact]
    public void Archive_url_asks_only_for_archived_fields()
    {
        string url = DayQuery.Url(52.52, 13.41, new DateOnly(1990, 7, 4), DaySource.Archive);
        Assert.StartsWith(DayQuery.ArchiveHost, url);
        Assert.Contains("start_date=1990-07-04&end_date=1990-07-04", url);
        Assert.DoesNotContain("precipitation_probability", url);
        Assert.Contains("precipitation_probability_max", DayQuery.Url(52.52, 13.41, Today, DaySource.Forecast));
    }

    [Fact]
    public void Parses_a_real_archive_day()
    {
        var json = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures", "berlin-1990-07-04.json"));
        var r = DayQuery.Parse(json, DaySource.Archive);
        Assert.Equal(new DateOnly(1990, 7, 4), r.Date);
        Assert.Equal(53, r.Code);
        Assert.Equal(19.5, r.MaxC);
        Assert.Equal(12.4, r.MinC);
        Assert.Equal(2.1, r.PrecipitationMm);
        Assert.Null(r.PrecipitationChance);
        Assert.Null(r.UvIndexMax);
        Assert.Equal(new DateTime(1990, 7, 4, 3, 49, 0), r.Sunrise);
        Assert.Equal(24, r.Hours.Count);
        Assert.True(r.Hours[12].IsDay);
    }

    [Fact]
    public void Error_answer_throws_with_the_reason()
    {
        var ex = Assert.Throws<FormatException>(() => DayQuery.Parse("""{"reason":"Parameter 'start_date' is out of allowed range","error":true}""", DaySource.Forecast));
        Assert.Contains("start_date", ex.Message);
    }
}
