namespace SkyGlance.Core.Places;

/// <summary>
/// Moves through "my location" (index -1) followed by the saved cities (0 to count - 1), wrapping at both ends.
/// </summary>
public static class CityCycle
{
    /// <summary>The index <paramref name="steps"/> places after <paramref name="current"/> (negative goes back).</summary>
    public static int Step(int current, int count, int steps)
    {
        int total = count + 1;              // my location + saved cities
        int pos = Clamp(current, count) + 1; // 0 = my location
        pos = ((pos + steps) % total + total) % total;
        return pos - 1;
    }

    /// <summary>A stored index made valid: anything out of range falls back to my location.</summary>
    public static int Clamp(int current, int count) => current >= 0 && current < count ? current : -1;

    /// <summary>The active index after removing city <paramref name="removed"/>.</summary>
    public static int AfterRemove(int current, int removed) =>
        current == removed ? -1 : current > removed ? current - 1 : current;
}
