using System.Text.Json;

namespace SkyGlance.Services;

/// <summary>Keeps the last forecast on disk so the widget has something to show at start-up and while offline.</summary>
static class ForecastStore
{
    public static Forecast? Load()
    {
        try
        {
            if (File.Exists(AppPaths.Forecast))
                return JsonSerializer.Deserialize<Forecast>(File.ReadAllText(AppPaths.Forecast));
        }
        catch (Exception ex) { Log.Error(ex); }
        return null;
    }

    public static void Save(Forecast f)
    {
        try
        {
            Directory.CreateDirectory(AppPaths.Data);
            var tmp = AppPaths.Forecast + ".tmp";
            File.WriteAllText(tmp, JsonSerializer.Serialize(f));
            File.Move(tmp, AppPaths.Forecast, true);
        }
        catch (Exception ex) { Log.Error(ex); }
    }
}
