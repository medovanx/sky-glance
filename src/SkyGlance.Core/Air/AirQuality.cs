using System.Globalization;
using System.Text.Json;

namespace SkyGlance.Core.Air;

public enum AirCategory { Good, Fair, Moderate, Poor, VeryPoor, ExtremelyPoor }

public enum PollenType { Alder, Birch, Grass, Mugwort, Olive, Ragweed }

public enum PollenLevel { None, Low, Moderate, High, VeryHigh }

/// <summary>The air right now: the European air quality index, the main pollutants and, in Europe, pollen.</summary>
public sealed record AirReading(
    DateTime Time,
    int? EuropeanAqi,
    double? Pm25,
    double? Pm10,
    double? Ozone,
    double? NitrogenDioxide,
    IReadOnlyDictionary<PollenType, double> Pollen,
    DateTime FetchedUtc)
{
    public AirCategory? Category => EuropeanAqi is { } a ? AirQuality.CategoryOf(a) : null;

    /// <summary>The pollen type with the highest level now and that level; null outside Europe or when there is none.</summary>
    public (PollenType type, PollenLevel level)? TopPollen =>
        Pollen.Count == 0 ? null
        : Pollen.Select(p => (p.Key, AirQuality.LevelOf(p.Key, p.Value))).OrderByDescending(p => p.Item2).ThenBy(p => p.Key).First() is var top && top.Item2 > PollenLevel.None
            ? top : null;
}

/// <summary>
/// Open-Meteo's air quality API (free, no key, from the CAMS models). The index and pollutants cover the
/// whole world; pollen comes only for Europe and is missing elsewhere.
/// </summary>
public static class AirQuality
{
    public const string Host = "https://air-quality-api.open-meteo.com/v1/air-quality";

    static readonly (PollenType type, string field)[] PollenFields =
    [
        (PollenType.Alder, "alder_pollen"), (PollenType.Birch, "birch_pollen"), (PollenType.Grass, "grass_pollen"),
        (PollenType.Mugwort, "mugwort_pollen"), (PollenType.Olive, "olive_pollen"), (PollenType.Ragweed, "ragweed_pollen"),
    ];

    public static string Url(double latitude, double longitude) => string.Create(CultureInfo.InvariantCulture,
        $"{Host}?latitude={latitude:0.####}&longitude={longitude:0.####}&current=european_aqi,pm2_5,pm10,ozone,nitrogen_dioxide,{string.Join(',', PollenFields.Select(p => p.field))}&timezone=auto");

    public static AirReading Parse(string json, DateTime fetchedUtc)
    {
        using var doc = JsonDocument.Parse(json);
        var r = doc.RootElement;
        if (r.TryGetProperty("error", out var err) && err.ValueKind == JsonValueKind.True)
            throw new FormatException(r.TryGetProperty("reason", out var why) ? why.GetString() : "Open-Meteo returned an error");
        var c = r.GetProperty("current");
        double? Num(string name) => c.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.Number ? v.GetDouble() : null;
        var pollen = new Dictionary<PollenType, double>();
        foreach (var (type, field) in PollenFields)
            if (Num(field) is { } g) pollen[type] = g;
        var time = c.TryGetProperty("time", out var t) && t.GetString() is { } ts
            ? DateTime.ParseExact(ts, ["yyyy-MM-dd'T'HH:mm", "yyyy-MM-dd'T'HH:mm:ss"], CultureInfo.InvariantCulture, DateTimeStyles.None)
            : DateTime.MinValue;
        return new AirReading(time, Num("european_aqi") is { } a ? (int)Math.Round(a) : null,
            Num("pm2_5"), Num("pm10"), Num("ozone"), Num("nitrogen_dioxide"), pollen, fetchedUtc);
    }

    /// <summary>The European Environment Agency's bands for the index: 0-20 good up to above 100 extremely poor.</summary>
    public static AirCategory CategoryOf(int aqi) => aqi switch
    {
        <= 20 => AirCategory.Good,
        <= 40 => AirCategory.Fair,
        <= 60 => AirCategory.Moderate,
        <= 80 => AirCategory.Poor,
        <= 100 => AirCategory.VeryPoor,
        _ => AirCategory.ExtremelyPoor,
    };

    /// <summary>
    /// Grains per cubic metre to a level. Weed pollen (mugwort, ragweed) bothers people at far lower counts than
    /// tree and grass pollen, so it has lower bands. Roughly the bands European pollen services use.
    /// </summary>
    public static PollenLevel LevelOf(PollenType type, double grains)
    {
        (double low, double moderate, double high, double veryHigh) b = type is PollenType.Mugwort or PollenType.Ragweed
            ? (1, 10, 30, 100)
            : type == PollenType.Grass ? (1, 20, 50, 200) : (1, 30, 100, 500);
        return grains < b.low ? PollenLevel.None
             : grains < b.moderate ? PollenLevel.Low
             : grains < b.high ? PollenLevel.Moderate
             : grains < b.veryHigh ? PollenLevel.High
             : PollenLevel.VeryHigh;
    }
}
