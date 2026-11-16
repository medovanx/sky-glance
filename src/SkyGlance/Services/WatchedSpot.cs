namespace SkyGlance.Services;

/// <summary>A place on the world watch-list, with its name in both languages.</summary>
public sealed record WatchedSpot(string En, string Ar, double Lat, double Lon)
{
    public string Name => L.Ar ? Ar : En;
}
