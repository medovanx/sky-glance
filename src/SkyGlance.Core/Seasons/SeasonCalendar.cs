namespace SkyGlance.Core.Seasons;

/// <summary>
/// Meteorological seasons: whole months, March to May is spring in the northern hemisphere and autumn in the
/// southern one, and so on. The hemisphere comes from the latitude of the place shown.
/// </summary>
public static class SeasonCalendar
{
    public static Season Of(DateOnly date, double latitude)
    {
        var north = date.Month switch
        {
            3 or 4 or 5 => Season.Spring,
            6 or 7 or 8 => Season.Summer,
            9 or 10 or 11 => Season.Autumn,
            _ => Season.Winter,
        };
        return latitude >= 0 ? north : (Season)(((int)north + 2) % 4);
    }

    /// <summary>The first day of the next season (the 1st of March, June, September or December).</summary>
    public static DateOnly NextStart(DateOnly date)
    {
        int m = date.Month;
        int next = m < 3 ? 3 : m < 6 ? 6 : m < 9 ? 9 : m < 12 ? 12 : 15;
        return next == 15 ? new DateOnly(date.Year + 1, 3, 1) : new DateOnly(date.Year, next, 1);
    }

    /// <summary>Days left in the current season, counting today.</summary>
    public static int DaysLeft(DateOnly date) => NextStart(date).DayNumber - date.DayNumber;
}
