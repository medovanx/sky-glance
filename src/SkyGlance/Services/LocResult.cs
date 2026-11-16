namespace SkyGlance.Services;

/// <summary>A place: coordinates and a display name such as "Lisbon, Portugal".</summary>
public sealed record LocResult(double Lat, double Lon, string Name);
