using System.Globalization;
using System.Text.Json;
using SkyGlance.Core.Weather;

namespace SkyGlance.Core.World;

/// <summary>
/// The hottest and coldest of a list of watched spots right now, from one Open-Meteo request for all of them.
/// Open-Meteo has no "hottest place on Earth" query, so the app watches a fixed list of the world's usual extremes.
/// </summary>
public sealed record Extremes(SpotReading Hottest, SpotReading Coldest, int Watched, DateTime FetchedUtc)
{
    public static string Url(IEnumerable<(double lat, double lon)> spots)
    {
        var list = spots.ToList();
        string lats = string.Join(",", list.Select(s => s.lat.ToString("0.##", CultureInfo.InvariantCulture)));
        string lons = string.Join(",", list.Select(s => s.lon.ToString("0.##", CultureInfo.InvariantCulture)));
        return $"{OpenMeteo.Host}?latitude={lats}&longitude={lons}&current=temperature_2m,weather_code,is_day&timezone=GMT";
    }

    /// <summary>Readings in request order. A single spot comes back as one object, several as an array.</summary>
    public static IReadOnlyList<SpotReading> ParseReadings(string json)
    {
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        var items = root.ValueKind == JsonValueKind.Array ? root.EnumerateArray().ToList() : [root];
        var list = new List<SpotReading>();
        for (int i = 0; i < items.Count; i++)
        {
            var e = items[i];
            if (!e.TryGetProperty("current", out var c) || !c.TryGetProperty("temperature_2m", out var t) || t.ValueKind != JsonValueKind.Number) continue;
            int index = e.TryGetProperty("location_id", out var id) && id.ValueKind == JsonValueKind.Number ? id.GetInt32() : i;
            int code = c.TryGetProperty("weather_code", out var w) && w.ValueKind == JsonValueKind.Number ? w.GetInt32() : 0;
            bool day = c.TryGetProperty("is_day", out var d) && d.ValueKind == JsonValueKind.Number && d.GetInt32() == 1;
            list.Add(new SpotReading(index, t.GetDouble(), code, day));
        }
        return list;
    }

    /// <summary>The hottest and coldest reading, or null when there is none.</summary>
    public static Extremes? Pick(IReadOnlyList<SpotReading> readings, int watched, DateTime fetchedUtc)
    {
        if (readings.Count == 0) return null;
        var hot = readings.MaxBy(r => r.TemperatureC)!;
        var cold = readings.MinBy(r => r.TemperatureC)!;
        return new Extremes(hot, cold, watched, fetchedUtc);
    }
}
