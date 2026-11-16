using System.Text.Json;

namespace SkyGlance.Services;

/// <summary>
/// Keeps the last forecast of each place on disk (keyed by <see cref="Place.Key"/>), so the widget has something
/// to show at start-up, while offline and right after switching city.
/// </summary>
static class ForecastStore
{
    public static Dictionary<string, Forecast> Load()
    {
        try
        {
            if (File.Exists(AppPaths.Forecasts))
                return JsonSerializer.Deserialize<Dictionary<string, Forecast>>(File.ReadAllText(AppPaths.Forecasts)) ?? [];
        }
        catch (Exception ex) { Log.Error(ex); }
        return [];
    }

    public static void Save(Dictionary<string, Forecast> all)
    {
        try
        {
            Directory.CreateDirectory(AppPaths.Data);
            var tmp = AppPaths.Forecasts + ".tmp";
            File.WriteAllText(tmp, JsonSerializer.Serialize(all));
            File.Move(tmp, AppPaths.Forecasts, true);
            if (File.Exists(AppPaths.OldForecast)) File.Delete(AppPaths.OldForecast);
        }
        catch (Exception ex) { Log.Error(ex); }
    }
}
