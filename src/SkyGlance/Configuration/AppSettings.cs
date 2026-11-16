using System.Globalization;
using System.Text.Json;
using Microsoft.Win32;

namespace SkyGlance.Configuration;

public sealed class AppSettings
{
    public string Language { get; set; } = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "ar" ? "ar" : "en";

    /// <summary>Find the location automatically (Windows location, then IP) instead of using the chosen city.</summary>
    public bool AutoLocation { get; set; } = true;
    public bool HasLocation { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public string LocationName { get; set; } = "";
    public DateTime LocationUpdatedUtc { get; set; }

    /// <summary>Extra cities to switch between, after "my location".</summary>
    public List<Place> Cities { get; set; } = [];
    /// <summary>The city shown: -1 for my location, otherwise an index into <see cref="Cities"/>.</summary>
    public int ActiveCity { get; set; } = -1;

    public TemperatureUnit Temperature { get; set; } = DefaultFahrenheit() ? TemperatureUnit.Fahrenheit : TemperatureUnit.Celsius;
    public WindUnit Wind { get; set; } = DefaultFahrenheit() ? WindUnit.MilesPerHour : WindUnit.KilometresPerHour;
    public PrecipitationUnit Precipitation { get; set; } = DefaultFahrenheit() ? PrecipitationUnit.Inches : PrecipitationUnit.Millimetres;

    /// <summary>Show the condition ("Light rain") under the temperature in the taskbar.</summary>
    public bool ShowCondition { get; set; } = true;
    /// <summary>Show today's high and low beside the temperature in the taskbar.</summary>
    public bool ShowHighLow { get; set; } = true;
    /// <summary>Show the hottest and coldest places on Earth right now in the flyout.</summary>
    public bool ShowExtremes { get; set; } = true;
    /// <summary>Show the season at the place shown in the flyout.</summary>
    public bool ShowSeason { get; set; } = true;
    /// <summary>Minutes between forecast refreshes.</summary>
    public int RefreshMinutes { get; set; } = 15;

    /// <summary>"Right" = beside the tray, "Left" = left end of the taskbar.</summary>
    public string WidgetSide { get; set; } = "Right";
    /// <summary>Extra shift in pixels (at 100% scale), positive = towards the centre.</summary>
    public int WidgetOffset { get; set; }

    public bool AutoCheckUpdates { get; set; } = true;
    public bool AutoInstallUpdates { get; set; } = true;
    public DateTime LastUpdateCheckUtc { get; set; }

    /// <summary>Fahrenheit by default only where it is the everyday unit.</summary>
    static bool DefaultFahrenheit()
    {
        try { return new RegionInfo(CultureInfo.CurrentCulture.Name).TwoLetterISORegionName is "US" or "LR" or "BS" or "BZ" or "KY" or "PW"; }
        catch { return false; }
    }

    static readonly JsonSerializerOptions Json = new() { WriteIndented = true };

    public static AppSettings Load()
    {
        try
        {
            if (File.Exists(AppPaths.Settings))
                return JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(AppPaths.Settings)) ?? new();
        }
        catch { /* corrupt file: fall back to defaults */ }
        return new();
    }

    public void Save()
    {
        Directory.CreateDirectory(AppPaths.Data);
        var tmp = AppPaths.Settings + ".tmp";
        File.WriteAllText(tmp, JsonSerializer.Serialize(this, Json));
        File.Move(tmp, AppPaths.Settings, true);
    }

    public AppSettings Clone() => JsonSerializer.Deserialize<AppSettings>(JsonSerializer.Serialize(this))!;

    // Autostart lives in the registry, not the settings file.
    const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
    const string RunName = "SkyGlance";

    public static bool StartWithWindows
    {
        get
        {
            using var k = Registry.CurrentUser.OpenSubKey(RunKey);
            return k?.GetValue(RunName) is string;
        }
        set
        {
            using var k = Registry.CurrentUser.CreateSubKey(RunKey);
            if (value) k.SetValue(RunName, $"\"{Environment.ProcessPath}\"");
            else k.DeleteValue(RunName, false);
        }
    }
}
