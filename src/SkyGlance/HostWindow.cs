namespace SkyGlance;

/// <summary>Hidden top-level window: receives TaskbarCreated (Explorer restart) and owns menus.</summary>
sealed class HostWindow : Form
{
    readonly SkyGlanceApp app;
    readonly uint taskbarCreated = Native.RegisterWindowMessage("TaskbarCreated");
    readonly uint showFlyout = Native.RegisterWindowMessage(SkyGlanceApp.ShowFlyoutMessage);

    public HostWindow(SkyGlanceApp app)
    {
        this.app = app;
        ShowInTaskbar = false;
        _ = Handle;
    }

    protected override void SetVisibleCore(bool value) => base.SetVisibleCore(false);

    protected override void WndProc(ref Message m)
    {
        if (m.Msg == (int)taskbarCreated) app.OnTaskbarCreated();
        else if (m.Msg == (int)showFlyout) app.ShowFlyoutFromOutside();
        base.WndProc(ref m);
    }
}
