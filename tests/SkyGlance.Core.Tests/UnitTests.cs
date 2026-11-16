using SkyGlance.Core.Units;

namespace SkyGlance.Core.Tests;

public class UnitTests
{
    [Theory]
    [InlineData(0, TemperatureUnit.Fahrenheit, "32\u00B0")]
    [InlineData(24.4, TemperatureUnit.Celsius, "24\u00B0")]
    [InlineData(24.5, TemperatureUnit.Celsius, "25\u00B0")]
    [InlineData(-3.6, TemperatureUnit.Celsius, "-4\u00B0")]
    [InlineData(-0.4, TemperatureUnit.Celsius, "0\u00B0")]
    [InlineData(-40, TemperatureUnit.Fahrenheit, "-40\u00B0")]
    public void Formats_temperature(double c, TemperatureUnit u, string expected) => Assert.Equal(expected, UnitFormat.Temperature(c, u));

    [Theory]
    [InlineData(36, WindUnit.KilometresPerHour, "36 km/h")]
    [InlineData(36, WindUnit.MetresPerSecond, "10 m/s")]
    [InlineData(16.2, WindUnit.MetresPerSecond, "4.5 m/s")]
    [InlineData(16.09344, WindUnit.MilesPerHour, "10 mph")]
    [InlineData(18.52, WindUnit.Knots, "10 kn")]
    [InlineData(30, WindUnit.Beaufort, "5 Bft")]
    public void Formats_wind(double kmh, WindUnit u, string expected) => Assert.Equal(expected, UnitFormat.Wind(kmh, u));

    [Theory]
    [InlineData(0, 0)]
    [InlineData(0.9, 0)]
    [InlineData(1, 1)]
    [InlineData(117, 12)]
    [InlineData(200, 12)]
    public void Beaufort_scale(double kmh, int force) => Assert.Equal(force, UnitConvert.Beaufort(kmh));

    [Theory]
    [InlineData(0, 0)]
    [InlineData(22, 0)]
    [InlineData(23, 1)]
    [InlineData(249, 6)]
    [InlineData(240, 5)]
    [InlineData(338, 0)]
    [InlineData(-90, 6)]
    [InlineData(720, 0)]
    public void Compass_points(int deg, int point) => Assert.Equal(point, UnitConvert.Compass(deg));

    [Fact]
    public void Precipitation_in_both_units()
    {
        Assert.Equal("1.2 mm", UnitFormat.Precipitation(1.2, PrecipitationUnit.Millimetres));
        Assert.Equal("12 mm", UnitFormat.Precipitation(12.3, PrecipitationUnit.Millimetres));
        Assert.Equal("1.00 in", UnitFormat.Precipitation(25.4, PrecipitationUnit.Inches));
    }
}
