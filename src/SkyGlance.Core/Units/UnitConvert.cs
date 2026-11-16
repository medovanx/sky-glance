namespace SkyGlance.Core.Units;

/// <summary>Converts the metric values Open-Meteo sends into the units the user picked.</summary>
public static class UnitConvert
{
    public static double Temperature(double celsius, TemperatureUnit unit) =>
        unit == TemperatureUnit.Fahrenheit ? celsius * 9 / 5 + 32 : celsius;

    public static double Wind(double kmh, WindUnit unit) => unit switch
    {
        WindUnit.MetresPerSecond => kmh / 3.6,
        WindUnit.MilesPerHour => kmh / 1.609344,
        WindUnit.Knots => kmh / 1.852,
        WindUnit.Beaufort => Beaufort(kmh),
        _ => kmh,
    };

    /// <summary>Beaufort force 0 to 12 from the upper limits of each force in km/h.</summary>
    public static int Beaufort(double kmh)
    {
        double[] limits = [1, 5, 11, 19, 28, 38, 49, 61, 74, 88, 102, 117];
        for (int i = 0; i < limits.Length; i++) if (kmh < limits[i]) return i;
        return 12;
    }

    public static double Precipitation(double mm, PrecipitationUnit unit) =>
        unit == PrecipitationUnit.Inches ? mm / 25.4 : mm;

    /// <summary>Rounds to a whole number without ever giving "-0".</summary>
    public static int Whole(double v)
    {
        int r = (int)Math.Round(v, MidpointRounding.AwayFromZero);
        return r == 0 ? 0 : r;
    }

    /// <summary>Compass point 0 to 7 (N, NE, E, SE, S, SW, W, NW) for a direction in degrees.</summary>
    public static int Compass(int degrees) => (int)Math.Round((((degrees % 360) + 360) % 360) / 45.0) % 8;
}
