namespace SkyGlance;

static class Program
{
    [STAThread]
    static int Main(string[] args)
    {
        if (args.Length > 1 && args[0] == "--shots")
            return Shots.Run(args[1]);
        if (args.Length > 1 && args[0] == "--render-icon")
            return IconMaker.Run(args[1]);

        using var mutex = new Mutex(false, @"Local\SkyGlance.SingleInstance");
        bool created;
        try
        {
            // After an update the old version is still exiting: wait for it to let go.
            created = mutex.WaitOne(args.Contains("--updated") || args.Contains("--restarted") ? TimeSpan.FromSeconds(20) : TimeSpan.Zero);
        }
        catch (AbandonedMutexException) { created = true; }
        if (!created)
        {
            // Already running: launching again opens the forecast flyout.
            Native.AllowSetForegroundWindow(-1); // ASFW_ANY: let the running instance take focus
            Native.PostMessage((IntPtr)0xFFFF, Native.RegisterWindowMessage(SkyGlanceApp.ShowFlyoutMessage), IntPtr.Zero, IntPtr.Zero);
            return 0;
        }

        ApplicationConfiguration.Initialize();
        Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
        Application.ThreadException += (_, e) => Log.Error(e.Exception);
        AppDomain.CurrentDomain.UnhandledException += (_, e) => Log.Error(e.ExceptionObject as Exception);
        try
        {
            Application.Run(new SkyGlanceApp());
        }
        catch (Exception ex)
        {
            // Something escaped the message loop (a WinForms message filter, for example). Log it and come back,
            // once, rather than vanishing from the taskbar.
            Log.Error(ex);
            if (!args.Contains("--restarted"))
                try { System.Diagnostics.Process.Start(Environment.ProcessPath!, "--restarted"); } catch { }
            return 1;
        }
        return 0;
    }
}
