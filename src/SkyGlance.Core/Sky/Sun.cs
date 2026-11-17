namespace SkyGlance.Core.Sky;

/// <summary>A golden hour: when the sun is low, from 4° below the horizon to 6° above it. Times are local to the place.</summary>
public sealed record GoldenHour(DateTime Start, DateTime End, bool Evening);

/// <summary>
/// The sun's height above the horizon (NOAA's solar position equations, good to well under a degree),
/// and the golden hours that follow from it.
/// </summary>
public static class Sun
{
    public const double GoldenLow = -4, GoldenHigh = 6;

    /// <summary>Degrees above the horizon (negative below) at <paramref name="utc"/>, seen from the given place.</summary>
    public static double Elevation(DateTime utc, double latitude, double longitude)
    {
        utc = DateTime.SpecifyKind(utc, DateTimeKind.Utc);
        double jd = utc.ToOADate() + 2415018.5;
        double t = (jd - 2451545.0) / 36525.0;

        double l0 = Norm(280.46646 + t * (36000.76983 + t * 0.0003032));
        double m = 357.52911 + t * (35999.05029 - 0.0001537 * t);
        double e = 0.016708634 - t * (0.000042037 + 0.0000001267 * t);
        double c = Sin(m) * (1.914602 - t * (0.004817 + 0.000014 * t)) + Sin(2 * m) * (0.019993 - 0.000101 * t) + Sin(3 * m) * 0.000289;
        double trueLong = l0 + c;
        double omega = 125.04 - 1934.136 * t;
        double appLong = trueLong - 0.00569 - 0.00478 * Sin(omega);
        double obliq0 = 23 + (26 + (21.448 - t * (46.815 + t * (0.00059 - t * 0.001813))) / 60) / 60;
        double obliq = obliq0 + 0.00256 * Cos(omega);
        double decl = Deg(Math.Asin(Sin(obliq) * Sin(appLong)));

        double y = Math.Pow(Math.Tan(Rad(obliq / 2)), 2);
        double eqTime = 4 * Deg(y * Sin(2 * l0) - 2 * e * Sin(m) + 4 * e * y * Sin(m) * Cos(2 * l0)
                              - 0.5 * y * y * Sin(4 * l0) - 1.25 * e * e * Sin(2 * m)); // minutes

        double minutes = utc.TimeOfDay.TotalMinutes;
        double trueSolar = (minutes + eqTime + 4 * longitude) % 1440;
        if (trueSolar < 0) trueSolar += 1440;
        double hourAngle = trueSolar / 4 < 0 ? trueSolar / 4 + 180 : trueSolar / 4 - 180;

        double cosZenith = Sin(latitude) * Sin(decl) + Cos(latitude) * Cos(decl) * Cos(hourAngle);
        double zenith = Deg(Math.Acos(Math.Clamp(cosZenith, -1, 1)));
        return 90 - zenith;
    }

    /// <summary>
    /// The golden hours of a local day (morning and evening), found minute by minute. Near the poles a day can
    /// have none, one long one, or the sun can stay in the band past midnight; each is clipped to the day.
    /// </summary>
    public static IReadOnlyList<GoldenHour> GoldenHours(DateOnly day, double latitude, double longitude, int utcOffsetSeconds)
    {
        var list = new List<GoldenHour>();
        var localStart = day.ToDateTime(TimeOnly.MinValue);
        DateTime? start = null;
        double prev = double.NaN;
        bool startRising = false;
        for (int i = 0; i <= 1440; i++)
        {
            var local = localStart.AddMinutes(i);
            double el = Elevation(local.AddSeconds(-utcOffsetSeconds), latitude, longitude);
            bool inBand = el >= GoldenLow && el <= GoldenHigh && i < 1440;
            if (inBand && start == null) { start = local; startRising = double.IsNaN(prev) ? el < Elevation(local.AddMinutes(1).AddSeconds(-utcOffsetSeconds), latitude, longitude) : el > prev; }
            else if (!inBand && start is { } s)
            {
                list.Add(new GoldenHour(s, local, Evening: !startRising));
                start = null;
            }
            prev = el;
        }
        return list;
    }

    /// <summary>The golden hour going on now or coming next, today or tomorrow; null where there is none (polar day or night).</summary>
    public static GoldenHour? Next(DateTime localNow, double latitude, double longitude, int utcOffsetSeconds)
    {
        var today = DateOnly.FromDateTime(localNow);
        return GoldenHours(today, latitude, longitude, utcOffsetSeconds)
            .Concat(GoldenHours(today.AddDays(1), latitude, longitude, utcOffsetSeconds))
            .FirstOrDefault(g => g.End > localNow);
    }

    static double Norm(double d) { d %= 360; return d < 0 ? d + 360 : d; }
    static double Rad(double d) => d * Math.PI / 180;
    static double Deg(double r) => r * 180 / Math.PI;
    static double Sin(double d) => Math.Sin(Rad(d));
    static double Cos(double d) => Math.Cos(Rad(d));
}
