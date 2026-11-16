namespace SkyGlance.Services;

/// <summary>Fetches forecasts from Open-Meteo (free, no key).</summary>
public static class WeatherService
{
    static readonly HttpClient Http = CreateClient();

    static HttpClient CreateClient()
    {
        var c = new HttpClient { Timeout = TimeSpan.FromSeconds(20) };
        c.DefaultRequestHeaders.UserAgent.ParseAdd($"SkyGlance/{Updater.CurrentText}");
        return c;
    }

    public static async Task<Forecast> FetchAsync(double lat, double lon)
    {
        string json = await Http.GetStringAsync(OpenMeteo.Url(lat, lon));
        return OpenMeteo.Parse(json, DateTime.UtcNow);
    }
}
