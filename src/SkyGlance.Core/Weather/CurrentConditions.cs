namespace SkyGlance.Core.Weather;

/// <summary>Weather right now. Times are local to the forecast location; all values are metric.</summary>
public sealed record CurrentConditions(
    DateTime Time,
    double TemperatureC,
    double FeelsLikeC,
    int Humidity,
    bool IsDay,
    int Code,
    double WindKmh,
    int WindDirection,
    double GustKmh,
    double PrecipitationMm,
    double PressureHpa,
    int CloudCover);
