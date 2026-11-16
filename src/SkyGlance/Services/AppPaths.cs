namespace SkyGlance.Services;

/// <summary>Where Sky Glance keeps its files: %APPDATA%\SkyGlance.</summary>
static class AppPaths
{
    public static readonly string Data = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "SkyGlance");
    public static string Settings => Path.Combine(Data, "settings.json");
    /// <summary>The last forecast of every place.</summary>
    public static string Forecasts => Path.Combine(Data, "forecasts.json");
    /// <summary>Single-place cache written by early builds; removed on the next save.</summary>
    public static string OldForecast => Path.Combine(Data, "forecast.json");
}
