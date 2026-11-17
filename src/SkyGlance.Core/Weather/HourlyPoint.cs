namespace SkyGlance.Core.Weather;

/// <summary>One hour of the forecast, starting at <see cref="Time"/> (local to the location).</summary>
/// <remarks><see cref="PrecipitationMm"/> is filled only for a single day from the calendar.</remarks>
public sealed record HourlyPoint(DateTime Time, double TemperatureC, int Code, int PrecipitationChance, bool IsDay, double PrecipitationMm = 0);
