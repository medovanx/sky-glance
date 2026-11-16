namespace SkyGlance.Core.Weather;

/// <summary>
/// WMO weather interpretation codes as Open-Meteo returns them (0 clear sky up to 99 thunderstorm with heavy hail).
/// Each code maps to a picture and to a text key ("Wmo61") that the app translates.
/// </summary>
public static class WeatherCode
{
    static readonly int[] Known = [0, 1, 2, 3, 45, 48, 51, 53, 55, 56, 57, 61, 63, 65, 66, 67, 71, 73, 75, 77, 80, 81, 82, 85, 86, 95, 96, 99];

    public static bool IsKnown(int code) => Array.IndexOf(Known, code) >= 0;

    public static IReadOnlyList<int> All => Known;

    /// <summary>Text key for the long description, or "WmoUnknown".</summary>
    public static string Key(int code) => IsKnown(code) ? $"Wmo{code}" : "WmoUnknown";

    public static SkyIcon Icon(int code) => code switch
    {
        0 => SkyIcon.Clear,
        1 or 2 => SkyIcon.PartlyCloudy,
        3 => SkyIcon.Cloudy,
        45 or 48 => SkyIcon.Fog,
        51 or 53 or 55 => SkyIcon.Drizzle,
        56 or 57 or 66 or 67 => SkyIcon.Sleet,
        61 or 63 or 65 or 80 or 81 or 82 => SkyIcon.Rain,
        71 or 73 or 75 or 77 or 85 or 86 => SkyIcon.Snow,
        95 or 96 or 99 => SkyIcon.Thunder,
        _ => SkyIcon.Cloudy,
    };

    /// <summary>True for codes that mean something falls from the sky.</summary>
    public static bool IsWet(int code) => code >= 51;
}
