using System.Text.Json;
using SkyGlance.Core.Air;
using SkyGlance.Core.Sky;
using SkyGlance.Core.Units;
using SkyGlance.Core.Weather;

namespace SkyGlance.Core.Tests;

public class RainAlertTests
{
    static readonly DateTime T0 = new(2026, 11, 17, 14, 0, 0);

    static List<RainSlot> Series(params double[] mm) => mm.Select((m, i) => new RainSlot(T0.AddMinutes(15 * i), m)).ToList();

    [Fact]
    public void DryNowRainInHalfAnHour_Starts()
    {
        var c = RainAlert.Check(Series(0, 0, 0.4, 0.8, 0.5, 0.2), T0.AddMinutes(5));
        Assert.NotNull(c);
        Assert.True(c!.Starting);
        Assert.Equal(T0.AddMinutes(30), c.At);
    }

    [Fact]
    public void RainingNowStopsSoon_Stops()
    {
        var c = RainAlert.Check(Series(0.6, 0.3, 0, 0, 0, 0), T0.AddMinutes(1));
        Assert.NotNull(c);
        Assert.False(c!.Starting);
        Assert.Equal(T0.AddMinutes(30), c.At);
    }

    [Fact]
    public void OneWetSlotIsNoAlert() => Assert.Null(RainAlert.Check(Series(0, 0.3, 0, 0, 0, 0), T0));

    [Fact]
    public void TracesUnderATenthOfAMillimetreAreDry() => Assert.Null(RainAlert.Check(Series(0, 0.05, 0.09, 0.05, 0, 0), T0));

    [Fact]
    public void RainMoreThanAnHourAwayWaits() => Assert.Null(RainAlert.Check(Series(0, 0, 0, 0, 0, 0.5, 0.6, 0.7), T0));

    [Fact]
    public void SeriesThatDoesNotCoverNowGivesNothing()
    {
        Assert.Null(RainAlert.Check(Series(0, 0.5, 0.5, 0.5), T0.AddHours(-1)));
        Assert.Null(RainAlert.Check(Series(0, 0.5, 0.5, 0.5), T0.AddHours(3)));
        Assert.Null(RainAlert.Check(null, T0));
    }
}

public class ForecastV2Tests
{
    static string Fixture(string name) => File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures", name));

    [Fact]
    public void ParsesFifteenMinuteRainAndYesterday()
    {
        var f = OpenMeteo.Parse(Fixture("berlin-v2.json"), new DateTime(2026, 11, 17, 4, 31, 0, DateTimeKind.Utc));
        Assert.Equal(16, f.Minutely!.Count);
        Assert.Equal(new DateTime(2026, 11, 17, 4, 30, 0), f.Minutely[0].Time);
        Assert.Equal(72, f.Hourly.Count); // 24 past hours and 48 ahead
        var localNow = f.LocalNow(f.FetchedUtc);
        Assert.Equal(new DateTime(2026, 11, 17, 5, 31, 0), localNow);
        // The hours shown still start now, not yesterday.
        Assert.Equal(new DateTime(2026, 11, 17, 5, 0, 0), f.HoursFrom(localNow, 1)[0].Time);
        var yesterday = f.Hourly.Single(h => h.Time == new DateTime(2026, 11, 16, 5, 0, 0)).TemperatureC;
        Assert.Equal(f.Current.TemperatureC - yesterday, f.ChangeSinceYesterday(localNow, f.Current.TemperatureC)!.Value, 6);
    }

    [Fact]
    public void UrlAsksForRainSlotsAndThePastDay()
    {
        string url = OpenMeteo.Url(52.52, 13.41);
        Assert.Contains("minutely_15=precipitation", url);
        Assert.Contains("past_hours=24", url);
        Assert.Contains("forecast_minutely_15=12", url);
    }

    [Fact]
    public void OldCacheWithoutTheNewFieldsStillLoads()
    {
        // A forecast saved by 1.2.4: no Minutely, and hourly data from now on only.
        var old = OpenMeteo.Parse(Fixture("berlin.json"), new DateTime(2026, 11, 16, 20, 0, 0, DateTimeKind.Utc)) with { Minutely = null };
        string json = JsonSerializer.Serialize(new Dictionary<string, Forecast> { ["x"] = old });
        using (var doc = JsonDocument.Parse(json)) Assert.False(doc.RootElement.GetProperty("x").TryGetProperty("Minutely", out var m) && m.ValueKind != JsonValueKind.Null);
        string withoutField = json.Replace(",\"Minutely\":null", "");
        var back = JsonSerializer.Deserialize<Dictionary<string, Forecast>>(withoutField)!["x"];
        Assert.Null(back.Minutely);
        Assert.Null(RainAlert.Check(back.Minutely, back.LocalNow(back.FetchedUtc)));
        Assert.Null(back.ChangeSinceYesterday(back.LocalNow(back.FetchedUtc), back.Current.TemperatureC));
    }

    [Fact]
    public void TemperatureDeltaScalesWithoutTheOffset()
    {
        Assert.Equal("3°", UnitFormat.TemperatureDelta(3, TemperatureUnit.Celsius));
        Assert.Equal("3°", UnitFormat.TemperatureDelta(-3, TemperatureUnit.Celsius));
        Assert.Equal("5°", UnitFormat.TemperatureDelta(3, TemperatureUnit.Fahrenheit));
        Assert.Equal("0°", UnitFormat.TemperatureDelta(0.2, TemperatureUnit.Fahrenheit));
    }
}

public class SkyTests
{
    [Theory]
    [InlineData(2024, 12, 15, 9, 2)]   // full moon
    [InlineData(2025, 9, 7, 18, 9)]    // full moon (eclipse)
    public void FullMoonsAreFull(int y, int mo, int d, int h, int mi)
    {
        var t = new DateTime(y, mo, d, h, mi, 0, DateTimeKind.Utc);
        Assert.Equal(MoonPhase.Full, Moon.Phase(t));
        Assert.True(Moon.Illumination(t) > 0.99);
    }

    [Fact]
    public void NewMoonIsDarkAndQuartersAreHalfLit()
    {
        var newMoon = new DateTime(2025, 1, 29, 12, 36, 0, DateTimeKind.Utc);
        Assert.Equal(MoonPhase.New, Moon.Phase(newMoon));
        Assert.True(Moon.Illumination(newMoon) < 0.01);
        var firstQuarter = new DateTime(2025, 2, 5, 8, 2, 0, DateTimeKind.Utc);
        Assert.Equal(MoonPhase.FirstQuarter, Moon.Phase(firstQuarter));
        Assert.InRange(Moon.Illumination(firstQuarter), 0.42, 0.58);
        Assert.True(Moon.Waxing(firstQuarter));
        Assert.Equal(MoonPhase.WaxingCrescent, Moon.Phase(newMoon.AddDays(3)));
        Assert.Equal(MoonPhase.WaningGibbous, Moon.Phase(new DateTime(2024, 12, 19, 0, 0, 0, DateTimeKind.Utc)));
    }

    [Fact]
    public void SunIsAtTheHorizonAtOpenMeteosSunriseAndSunset()
    {
        // Berlin, from the fixture: sunrise 07:31 and sunset 16:10 local (UTC+1) on 17 November 2026.
        var f = OpenMeteo.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures", "berlin-v2.json")), DateTime.UtcNow);
        var day = f.Daily.First(d => d.Date == new DateOnly(2026, 11, 17));
        foreach (var t in new[] { day.Sunrise!.Value, day.Sunset!.Value })
            Assert.InRange(Sun.Elevation(t.AddSeconds(-f.UtcOffsetSeconds), f.Latitude, f.Longitude), -1.5, 0.2); // refraction puts it near -0.83
    }

    [Fact]
    public void BerlinHasAMorningAndAnEveningGoldenHour()
    {
        var hours = Sun.GoldenHours(new DateOnly(2026, 11, 17), 52.52, 13.41, 3600);
        Assert.Equal(2, hours.Count);
        Assert.False(hours[0].Evening);
        Assert.True(hours[1].Evening);
        // Around sunrise (07:31) and sunset (16:10): starting before sunrise, ending after sunset.
        Assert.InRange(hours[0].Start.TimeOfDay, new TimeSpan(6, 40, 0), new TimeSpan(7, 30, 0));
        Assert.InRange(hours[1].End.TimeOfDay, new TimeSpan(16, 10, 0), new TimeSpan(17, 0, 0));
        var next = Sun.Next(new DateTime(2026, 11, 17, 12, 0, 0), 52.52, 13.41, 3600);
        Assert.True(next!.Evening);
    }

    [Fact]
    public void SunsetMatchesOpenMeteoWithinAFewMinutes()
    {
        var f = OpenMeteo.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures", "berlin-v2.json")), DateTime.UtcNow);
        var day = f.Daily.First(d => d.Date == new DateOnly(2026, 11, 17));
        var calc = Sun.Sunset(day.Date, f.Latitude, f.Longitude, f.UtcOffsetSeconds)!.Value;
        Assert.InRange((calc - day.Sunset!.Value).TotalMinutes, -3, 3);
    }

    [Fact]
    public void PolarNightHasNoGoldenHourAtTheNorthPole() =>
        Assert.Empty(Sun.GoldenHours(new DateOnly(2026, 12, 21), 89.9, 0, 0));
}

public class AirQualityTests
{
    static string Fixture(string name) => File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures", name));

    [Fact]
    public void ParsesIndexPollutantsAndPollenInEurope()
    {
        var a = AirQuality.Parse(Fixture("berlin-air.json"), DateTime.UtcNow);
        Assert.Equal(27, a.EuropeanAqi);
        Assert.Equal(AirCategory.Fair, a.Category);
        Assert.Equal(8.4, a.Pm25);
        Assert.Equal(6, a.Pollen.Count);
        Assert.Null(a.TopPollen); // November: none in the air
    }

    [Fact]
    public void PollenIsMissingOutsideEurope()
    {
        var a = AirQuality.Parse(Fixture("tokyo-air.json"), DateTime.UtcNow);
        Assert.Equal(47, a.EuropeanAqi);
        Assert.Equal(AirCategory.Moderate, a.Category);
        Assert.Empty(a.Pollen);
        Assert.Null(a.TopPollen);
    }

    [Theory]
    [InlineData(0, AirCategory.Good)]
    [InlineData(20, AirCategory.Good)]
    [InlineData(21, AirCategory.Fair)]
    [InlineData(60, AirCategory.Moderate)]
    [InlineData(61, AirCategory.Poor)]
    [InlineData(100, AirCategory.VeryPoor)]
    [InlineData(150, AirCategory.ExtremelyPoor)]
    public void Bands(int aqi, AirCategory c) => Assert.Equal(c, AirQuality.CategoryOf(aqi));

    [Fact]
    public void WeedPollenCountsAtLowerLevels()
    {
        Assert.Equal(PollenLevel.High, AirQuality.LevelOf(PollenType.Ragweed, 40));
        Assert.Equal(PollenLevel.Moderate, AirQuality.LevelOf(PollenType.Birch, 40));
        Assert.Equal(PollenLevel.None, AirQuality.LevelOf(PollenType.Grass, 0));
        var a = new AirReading(DateTime.MinValue, 10, null, null, null, null,
            new Dictionary<PollenType, double> { [PollenType.Birch] = 40, [PollenType.Ragweed] = 40 }, DateTime.UtcNow);
        Assert.Equal((PollenType.Ragweed, PollenLevel.High), a.TopPollen);
    }

    [Fact]
    public void UrlAsksForTheIndexAndPollen()
    {
        string url = AirQuality.Url(52.52, 13.41);
        Assert.StartsWith(AirQuality.Host, url);
        Assert.Contains("european_aqi", url);
        Assert.Contains("ragweed_pollen", url);
    }
}
