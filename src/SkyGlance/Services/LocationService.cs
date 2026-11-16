using System.Globalization;
using System.Text.Json;
using Windows.Devices.Geolocation;

namespace SkyGlance.Services;

/// <summary>
/// Windows location first; if that is off or fails, an IP lookup (ipwho.is).
/// City names for a Windows fix come from BigDataCloud's free reverse-geocode endpoint.
/// City search uses the Open-Meteo geocoding API. None of them need an API key.
/// </summary>
public static class LocationService
{
    static readonly HttpClient Http = CreateClient();

    static HttpClient CreateClient()
    {
        var c = new HttpClient { Timeout = TimeSpan.FromSeconds(12) };
        c.DefaultRequestHeaders.UserAgent.ParseAdd($"SkyGlance/{Updater.CurrentText}");
        return c;
    }

    public static async Task<LocResult?> DetectAsync(string lang) =>
        await FromWindowsAsync(lang) ?? await FromIpAsync();

    static async Task<LocResult?> FromWindowsAsync(string lang)
    {
        try
        {
            var access = await Geolocator.RequestAccessAsync();
            if (access != GeolocationAccessStatus.Allowed) return null;
            var g = new Geolocator { DesiredAccuracy = PositionAccuracy.Default };
            var pos = await g.GetGeopositionAsync(TimeSpan.FromMinutes(30), TimeSpan.FromSeconds(15));
            var p = pos.Coordinate.Point.Position;
            string name = await ReverseAsync(p.Latitude, p.Longitude, lang)
                          ?? string.Create(CultureInfo.InvariantCulture, $"{p.Latitude:F2}, {p.Longitude:F2}");
            return new(p.Latitude, p.Longitude, name);
        }
        catch { return null; }
    }

    static async Task<string?> ReverseAsync(double lat, double lon, string lang)
    {
        try
        {
            var url = string.Create(CultureInfo.InvariantCulture,
                $"https://api.bigdatacloud.net/data/reverse-geocode-client?latitude={lat}&longitude={lon}&localityLanguage={lang}");
            using var doc = JsonDocument.Parse(await Http.GetStringAsync(url));
            var r = doc.RootElement;
            string city = Str(r, "city") is { Length: > 0 } c ? c : Str(r, "locality");
            string country = Str(r, "countryName");
            return Join(city, country);
        }
        catch { return null; }
    }

    static async Task<LocResult?> FromIpAsync()
    {
        try
        {
            using var doc = JsonDocument.Parse(await Http.GetStringAsync("https://ipwho.is/?fields=success,city,country,latitude,longitude"));
            var r = doc.RootElement;
            if (!r.TryGetProperty("success", out var ok) || !ok.GetBoolean()) return null;
            return new(r.GetProperty("latitude").GetDouble(), r.GetProperty("longitude").GetDouble(),
                       Join(Str(r, "city"), Str(r, "country")) ?? "");
        }
        catch { return null; }
    }

    public static async Task<List<LocResult>> SearchAsync(string query, string lang)
    {
        var list = new List<LocResult>();
        try
        {
            var url = $"https://geocoding-api.open-meteo.com/v1/search?name={Uri.EscapeDataString(query)}&count=10&language={lang}&format=json";
            using var doc = JsonDocument.Parse(await Http.GetStringAsync(url));
            if (!doc.RootElement.TryGetProperty("results", out var results)) return list;
            foreach (var r in results.EnumerateArray())
            {
                string name = Join(Str(r, "name"), Str(r, "admin1"), Str(r, "country")) ?? "";
                list.Add(new(r.GetProperty("latitude").GetDouble(), r.GetProperty("longitude").GetDouble(), name));
            }
        }
        catch (Exception ex) { Log.Error(ex); /* offline: empty list */ }
        return list;
    }

    static string Str(JsonElement e, string prop) =>
        e.TryGetProperty(prop, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() ?? "" : "";

    static string? Join(params string[] parts)
    {
        var p = parts.Where(s => !string.IsNullOrWhiteSpace(s)).Distinct().ToArray();
        return p.Length == 0 ? null : string.Join(L.Ar ? "، " : ", ", p);
    }
}
