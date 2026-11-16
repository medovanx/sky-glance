namespace SkyGlance.Core.Weather;

/// <summary>One hour of the forecast, starting at <see cref="Time"/> (local to the location).</summary>
public sealed record HourlyPoint(DateTime Time, double TemperatureC, int Code, int PrecipitationChance, bool IsDay);
