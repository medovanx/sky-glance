namespace SkyGlance.Core.Weather;

/// <summary>
/// The weather of one calendar day at one place, in metric units. Rain chance and UV index only come with the
/// forecast; the archive records what fell instead.
/// </summary>
public sealed record DayReport(
    DateOnly Date,
    DaySource Source,
    int Code,
    double MaxC,
    double MinC,
    double PrecipitationMm,
    int? PrecipitationChance,
    double WindMaxKmh,
    double? UvIndexMax,
    DateTime? Sunrise,
    DateTime? Sunset,
    IReadOnlyList<HourlyPoint> Hours);
