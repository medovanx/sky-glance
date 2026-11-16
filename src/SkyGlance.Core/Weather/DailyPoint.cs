namespace SkyGlance.Core.Weather;

/// <summary>One day of the forecast. Sunrise and sunset are null near the poles when the sun never rises or sets.</summary>
public sealed record DailyPoint(
    DateOnly Date,
    int Code,
    double MaxC,
    double MinC,
    int PrecipitationChance,
    DateTime? Sunrise,
    DateTime? Sunset,
    double UvIndexMax,
    double WindMaxKmh);
