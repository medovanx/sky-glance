namespace SkyGlance.UI.Styling;

/// <summary>Colours shared by the flyout and the settings window.</summary>
sealed record Theme(bool Dark)
{
    public static Theme Current => new(Native.SystemDark());
    public static Theme Light => new(false);
    public Color Window => Dark ? Color.FromArgb(28, 31, 36) : Color.FromArgb(243, 245, 248);
    public Color Surface => Dark ? Color.FromArgb(38, 42, 48) : Color.White;
    public Color Input => Dark ? Color.FromArgb(50, 55, 62) : Color.FromArgb(246, 247, 250);
    public Color Border => Dark ? Color.FromArgb(60, 66, 74) : Color.FromArgb(220, 224, 230);
    public Color Text => Dark ? Color.FromArgb(238, 240, 244) : Color.FromArgb(22, 26, 32);
    public Color Dim => Dark ? Color.FromArgb(156, 164, 176) : Color.FromArgb(100, 108, 120);
    public Color Accent => Dark ? Color.FromArgb(110, 180, 250) : Color.FromArgb(22, 98, 190);
    public Color AccentFill => Color.FromArgb(28, 106, 200);
    public Color AccentHover => Color.FromArgb(44, 124, 220);
    /// <summary>Highs are warm, lows are cool, rain is blue, everywhere in the app.</summary>
    public Color Warm => Dark ? Color.FromArgb(251, 176, 64) : Color.FromArgb(214, 112, 10);
    public Color Cool => Dark ? Color.FromArgb(96, 196, 250) : Color.FromArgb(14, 128, 200);
    public Color Rain => Dark ? Color.FromArgb(110, 170, 255) : Color.FromArgb(30, 100, 220);
}
