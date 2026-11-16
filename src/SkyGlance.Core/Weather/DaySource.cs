namespace SkyGlance.Core.Weather;

/// <summary>Where the weather of a calendar day comes from.</summary>
public enum DaySource
{
    /// <summary>The forecast API: about three months back up to 15 days ahead.</summary>
    Forecast,
    /// <summary>The historical archive (ERA5 reanalysis): every day since 1940.</summary>
    Archive,
    /// <summary>Before 1940 or more than 15 days ahead: nobody has it.</summary>
    OutOfRange,
}
