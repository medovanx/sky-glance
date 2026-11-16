using SkyGlance.Core.Seasons;

namespace SkyGlance.Core.Tests;

public class SeasonTests
{
    [Theory]
    [InlineData(2026, 11, 16, 30.0, Season.Autumn)]
    [InlineData(2026, 11, 16, -33.9, Season.Spring)]
    [InlineData(2026, 12, 1, 52.5, Season.Winter)]
    [InlineData(2026, 12, 1, -33.9, Season.Summer)]
    [InlineData(2027, 2, 28, 0.0, Season.Winter)]
    [InlineData(2027, 3, 1, 10.0, Season.Spring)]
    [InlineData(2026, 7, 15, -1.0, Season.Winter)]
    public void Season_by_month_and_hemisphere(int y, int m, int d, double lat, Season expected) =>
        Assert.Equal(expected, SeasonCalendar.Of(new DateOnly(y, m, d), lat));

    [Fact]
    public void Days_left_until_the_next_season()
    {
        Assert.Equal(15, SeasonCalendar.DaysLeft(new DateOnly(2026, 11, 16)));
        Assert.Equal(new DateOnly(2027, 3, 1), SeasonCalendar.NextStart(new DateOnly(2026, 12, 25)));
        Assert.Equal(1, SeasonCalendar.DaysLeft(new DateOnly(2027, 2, 28)));
    }
}
