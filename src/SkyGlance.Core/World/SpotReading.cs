namespace SkyGlance.Core.World;

/// <summary>The temperature right now at watched spot <see cref="Index"/> (its position in the request).</summary>
public sealed record SpotReading(int Index, double TemperatureC, int Code, bool IsDay);
