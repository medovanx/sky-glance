namespace SkyGlance.Core.Weather;

/// <summary>Rain about to start or stop at the place, and when.</summary>
public sealed record RainChange(bool Starting, DateTime At);

/// <summary>
/// Decides from the 15-minute precipitation series whether rain starts or stops within the next hour.
/// A slot counts as wet from 0.1 mm. A start needs at least half an hour of rain to follow, and a stop needs
/// half an hour of dry weather after it, so a single wet or dry slot never raises an alert.
/// </summary>
public static class RainAlert
{
    public const double WetMm = 0.1;
    static readonly TimeSpan Slot = TimeSpan.FromMinutes(15);
    static readonly TimeSpan Horizon = TimeSpan.FromMinutes(60);

    static bool Wet(RainSlot s) => s.Mm >= WetMm;

    /// <returns>The change coming within the hour, or null when the weather stays as it is (or the data doesn't reach).</returns>
    public static RainChange? Check(IReadOnlyList<RainSlot>? slots, DateTime localNow)
    {
        if (slots is null || slots.Count == 0) return null;
        var ordered = slots.OrderBy(s => s.Time).ToList();
        int cur = ordered.FindLastIndex(s => s.Time <= localNow);
        if (cur < 0 || localNow - ordered[cur].Time >= Slot) return null; // the series doesn't cover now
        bool wetNow = Wet(ordered[cur]);

        for (int i = cur + 1; i < ordered.Count && ordered[i].Time - localNow <= Horizon; i++)
        {
            if (Wet(ordered[i]) == wetNow) continue;
            // The weather flips at slot i: it has to hold for the next two slots (half an hour) too.
            if (i + 2 >= ordered.Count) return null;
            if (Wet(ordered[i + 1]) == wetNow || Wet(ordered[i + 2]) == wetNow) continue;
            return new RainChange(!wetNow, ordered[i].Time);
        }
        return null;
    }
}
