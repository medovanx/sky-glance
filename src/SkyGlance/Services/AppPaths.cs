namespace SkyGlance.Services;

/// <summary>Where Sky Glance keeps its files: %APPDATA%\SkyGlance.</summary>
static class AppPaths
{
    public static readonly string Data = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "SkyGlance");
    public static string Settings => Path.Combine(Data, "settings.json");
    public static string Forecast => Path.Combine(Data, "forecast.json");
}
