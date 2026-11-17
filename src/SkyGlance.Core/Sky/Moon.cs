namespace SkyGlance.Core.Sky;

public enum MoonPhase { New, WaxingCrescent, FirstQuarter, WaxingGibbous, Full, WaningGibbous, LastQuarter, WaningCrescent }

/// <summary>
/// The moon's phase from its age in the synodic month, counted from a known new moon.
/// Good to a few hours, which is plenty for naming the phase and showing how much of it is lit.
/// </summary>
public static class Moon
{
    public const double SynodicDays = 29.530588853;
    /// <summary>New moon of 6 January 2000, 18:14 UTC.</summary>
    static readonly DateTime KnownNew = new(2000, 1, 6, 18, 14, 0, DateTimeKind.Utc);

    /// <summary>Days since the last new moon, 0 to 29.53.</summary>
    public static double Age(DateTime utc)
    {
        double days = (DateTime.SpecifyKind(utc, DateTimeKind.Utc) - KnownNew).TotalDays % SynodicDays;
        return days < 0 ? days + SynodicDays : days;
    }

    /// <summary>The lit fraction of the disc, 0 (new) to 1 (full).</summary>
    public static double Illumination(DateTime utc) => (1 - Math.Cos(2 * Math.PI * Age(utc) / SynodicDays)) / 2;

    /// <summary>True from new moon to full moon, when the lit side grows.</summary>
    public static bool Waxing(DateTime utc) => Age(utc) < SynodicDays / 2;

    /// <summary>
    /// The named phase. New, quarters and full each get about a day either side of the exact moment,
    /// the in-between phases the rest, as almanacs and phone apps name them.
    /// </summary>
    public static MoonPhase Phase(DateTime utc)
    {
        double a = Age(utc), q = SynodicDays / 4;
        if (a < 1 || a > SynodicDays - 1) return MoonPhase.New;
        if (Math.Abs(a - q) < 1) return MoonPhase.FirstQuarter;
        if (Math.Abs(a - 2 * q) < 1) return MoonPhase.Full;
        if (Math.Abs(a - 3 * q) < 1) return MoonPhase.LastQuarter;
        if (a < q) return MoonPhase.WaxingCrescent;
        if (a < 2 * q) return MoonPhase.WaxingGibbous;
        if (a < 3 * q) return MoonPhase.WaningGibbous;
        return MoonPhase.WaningCrescent;
    }
}
