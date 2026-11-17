namespace SkyGlance.Core.Weather;

/// <summary>Fifteen minutes of precipitation starting at <see cref="Time"/> (local to the location), in millimetres.</summary>
public sealed record RainSlot(DateTime Time, double Mm);
