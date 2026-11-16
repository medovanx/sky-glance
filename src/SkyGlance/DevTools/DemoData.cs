namespace SkyGlance.DevTools;

/// <summary>A made-up forecast for a demo city, for screenshots, so the user's real location never appears.</summary>
static class DemoData
{
    public const double Lat = 38.72, Lon = -9.14;

    public static string City(string lang) => lang == "ar" ? "لشبونة، البرتغال" : "Lisbon, Portugal";

    public static AppSettings Settings(string lang) => new()
    {
        Language = lang,
        AutoLocation = false,
        HasLocation = true,
        Latitude = Lat,
        Longitude = Lon,
        LocationName = City(lang),
        Temperature = TemperatureUnit.Celsius,
        Wind = WindUnit.KilometresPerHour,
        Precipitation = PrecipitationUnit.Millimetres,
    };

    /// <summary>
    /// A mild afternoon with showers coming in tomorrow evening. The offset is chosen so that the clock at the
    /// demo city reads 14:20 whenever the shots are rendered.
    /// </summary>
    public static Forecast Forecast()
    {
        var utc = DateTime.UtcNow;
        var today = DateTime.SpecifyKind(utc, DateTimeKind.Unspecified).Date;
        var nowLocal = today.AddHours(14).AddMinutes(20);
        int offset = (int)Math.Round((nowLocal - DateTime.SpecifyKind(utc, DateTimeKind.Unspecified)).TotalSeconds);

        var hourly = new List<HourlyPoint>();
        var start = today.AddHours(14);
        int[] codes = [2, 2, 1, 1, 0, 0, 0, 0, 0, 1, 1, 2, 2, 3, 3, 3, 3, 2, 2, 3, 3, 61, 80, 80, 61, 3, 3, 2, 2, 1, 1, 1, 0, 0, 0, 0, 1, 1, 2, 2, 2, 2, 2, 1, 1, 1, 1, 1];
        for (int i = 0; i < 48; i++)
        {
            var at = start.AddHours(i);
            double temp = 17 + 4.5 * Math.Cos((at.Hour - 15) / 24.0 * Math.PI * 2) - (i > 20 ? 1.5 : 0);
            int chance = codes[i] >= 61 ? 70 + i % 3 * 8 : codes[i] == 3 ? 15 : 0;
            hourly.Add(new HourlyPoint(at, Math.Round(temp, 1), codes[i], chance, at.Hour is >= 7 and < 18));
        }

        (int code, double max, double min, int rain)[] days =
            [(2, 21.6, 13.1, 5), (80, 19.2, 13.8, 76), (3, 18.4, 12.6, 30), (0, 22.3, 12.0, 0), (1, 23.5, 13.4, 0), (95, 20.1, 14.2, 64), (2, 19.7, 12.9, 12)];
        var daily = days.Select((d, i) => new DailyPoint(DateOnly.FromDateTime(today.AddDays(i)), d.code, d.max, d.min, d.rain,
            today.AddDays(i).AddHours(7).AddMinutes(18 + i), today.AddDays(i).AddHours(17).AddMinutes(52 - i), 4 + i % 3, 18 + i * 2)).ToList();

        var current = new CurrentConditions(nowLocal, 21.2, 20.6, 58, true, 2, 14.4, 315, 27.0, 0, 1017.2, 40);
        return new Forecast(Lat, Lon, "Europe/Lisbon", offset, utc.AddMinutes(-6), current, hourly, daily);
    }
}
