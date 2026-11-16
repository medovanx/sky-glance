using System.Globalization;

namespace SkyGlance.Core.Places;

/// <summary>A saved city: its display name and coordinates.</summary>
public sealed record Place(string Name, double Latitude, double Longitude)
{
    /// <summary>Identifies a place for the forecast cache; about 1 km of precision, so a re-detected location matches.</summary>
    public string Key => KeyOf(Latitude, Longitude);

    public static string KeyOf(double latitude, double longitude) =>
        string.Create(CultureInfo.InvariantCulture, $"{Math.Round(latitude, 2):0.00},{Math.Round(longitude, 2):0.00}");
}
