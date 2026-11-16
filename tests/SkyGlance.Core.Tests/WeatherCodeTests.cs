using SkyGlance.Core.Weather;

namespace SkyGlance.Core.Tests;

public class WeatherCodeTests
{
    [Theory]
    [InlineData(0, SkyIcon.Clear)]
    [InlineData(2, SkyIcon.PartlyCloudy)]
    [InlineData(3, SkyIcon.Cloudy)]
    [InlineData(48, SkyIcon.Fog)]
    [InlineData(53, SkyIcon.Drizzle)]
    [InlineData(66, SkyIcon.Sleet)]
    [InlineData(81, SkyIcon.Rain)]
    [InlineData(86, SkyIcon.Snow)]
    [InlineData(99, SkyIcon.Thunder)]
    public void Maps_codes_to_icons(int code, SkyIcon icon) => Assert.Equal(icon, WeatherCode.Icon(code));

    [Fact]
    public void Unknown_codes_get_a_fallback()
    {
        Assert.Equal("WmoUnknown", WeatherCode.Key(42));
        Assert.Equal(SkyIcon.Cloudy, WeatherCode.Icon(42));
        Assert.Equal("Wmo61", WeatherCode.Key(61));
    }

    [Fact]
    public void Every_known_code_has_its_own_key() =>
        Assert.Equal(WeatherCode.All.Count, WeatherCode.All.Select(WeatherCode.Key).Distinct().Count());
}
