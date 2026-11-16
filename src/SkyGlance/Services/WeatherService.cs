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

    /// <summary>The weather of one calendar day, from the forecast or the archive.</summary>
    public static async Task<DayReport> FetchDayAsync(double lat, double lon, DateOnly date, DaySource source) =>
        DayQuery.Parse(await Http.GetStringAsync(DayQuery.Url(lat, lon, date, source)), source);

    /// <summary>The hottest and coldest of the world watch-list right now, in one request.</summary>
    public static async Task<Extremes?> FetchExtremesAsync()
    {
        string json = await Http.GetStringAsync(Extremes.Url(WorldWatch.Spots.Select(s => (s.Lat, s.Lon))));
        return Extremes.Pick(Extremes.ParseReadings(json), WorldWatch.Spots.Length, DateTime.UtcNow);
    }
}
