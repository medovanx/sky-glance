using System.Globalization;

namespace SkyGlance.Core.Units;

/// <summary>Short display strings for values. Digits are always Latin; the app adds direction marks for Arabic.</summary>
public static class UnitFormat
{
    static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

    /// <summary>"24°" or "-3°".</summary>
    public static string Temperature(double celsius, TemperatureUnit unit) =>
        UnitConvert.Whole(UnitConvert.Temperature(celsius, unit)).ToString(Inv) + "\u00B0";

    /// <summary>"°C" or "°F".</summary>
    public static string TemperatureSymbol(TemperatureUnit unit) => unit == TemperatureUnit.Fahrenheit ? "\u00B0F" : "\u00B0C";

    public static string WindSymbol(WindUnit unit) => unit switch
    {
        WindUnit.MetresPerSecond => "m/s",
        WindUnit.MilesPerHour => "mph",
        WindUnit.Knots => "kn",
        WindUnit.Beaufort => "Bft",
        _ => "km/h",
    };

    /// <summary>"16 km/h", "4.6 m/s" or "3 Bft".</summary>
    public static string Wind(double kmh, WindUnit unit)
    {
        double v = UnitConvert.Wind(kmh, unit);
        string n = unit == WindUnit.MetresPerSecond && v < 10 ? v.ToString("0.0", Inv) : UnitConvert.Whole(v).ToString(Inv);
        return $"{n} {WindSymbol(unit)}";
    }

    /// <summary>"1.2 mm" or "0.05 in".</summary>
    public static string Precipitation(double mm, PrecipitationUnit unit) => unit == PrecipitationUnit.Inches
        ? UnitConvert.Precipitation(mm, unit).ToString("0.00", Inv) + " in"
        : mm.ToString(mm < 10 ? "0.0" : "0", Inv) + " mm";

    public static string Percent(int value) => value.ToString(Inv) + "%";
}
