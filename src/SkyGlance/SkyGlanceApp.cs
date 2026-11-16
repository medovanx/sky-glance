using Microsoft.Win32;

namespace SkyGlance;

sealed class SkyGlanceApp : ApplicationContext
{
    public AppSettings S { get; private set; }
    public Icon AppIcon { get; }

    /// <summary>The last forecast of every place, fresh or from the cache, keyed by <see cref="Place.Key"/>.</summary>
    readonly Dictionary<string, Forecast> forecasts;

    /// <summary>The place shown: my location (detected or chosen) or one of the saved cities; null until my location is known.</summary>
    public Place? ActivePlace
    {
        get
        {
            int i = CityCycle.Clamp(S.ActiveCity, S.Cities.Count);
            if (i >= 0) return S.Cities[i];
            return S.HasLocation ? new Place(S.LocationName, S.Latitude, S.Longitude) : null;
        }
    }

    /// <summary>The forecast of the place shown, fresh or from the cache; null until the first one arrives.</summary>
    public Forecast? Forecast => ActivePlace is { } p && forecasts.TryGetValue(p.Key, out var f) ? f : null;
    /// <summary>The hottest and coldest watched places right now; null until fetched (hourly) or when turned off.</summary>
    public Extremes? World { get; private set; }
    DateTime nextWorldUtc = DateTime.MinValue;
    bool fetchingWorld;

    /// <summary>True when the last refresh failed (offline, Open-Meteo down).</summary>
    public bool LastFetchFailed { get; private set; }
    public bool Fetching { get; private set; }

    readonly HostWindow host;
    readonly TaskbarWidget widget = new();
    readonly NotifyIcon tray;
    readonly ContextMenuStrip menu = new();
    readonly System.Windows.Forms.Timer timer = new() { Interval = 2000 };
    readonly System.Windows.Forms.Timer updateTimer = new() { Interval = 45_000 };
    WeatherFlyout? flyout;
    SettingsForm? settingsForm;
    DateTime nextFetchUtc = DateTime.MinValue;
    int failures;

    /// <summary>Headless instance for --shots: no tray, widget, timers, network or saving.</summary>
    public bool Headless { get; }

    /// <summary>Raised when the forecast or the settings change.</summary>
    public event Action? Changed;

    public SkyGlanceApp(AppSettings? settings = null, Dictionary<string, Forecast>? demo = null, Extremes? demoWorld = null)
    {
        Headless = settings != null;
        S = settings ?? AppSettings.Load();
        L.Ar = S.Language == "ar";
        AppIcon = LoadIcon();
        forecasts = demo ?? (Headless ? [] : ForecastStore.Load());
        World = demoWorld;

        host = new HostWindow(this);
        menu.Opening += (_, e) => { BuildMenu(); e.Cancel = false; };
        tray = new NotifyIcon
        {
            Icon = new Icon(AppIcon, SystemInformation.SmallIconSize),
            Text = L.T("AppName"),
            ContextMenuStrip = menu,
            Visible = !Headless,
        };
        if (Headless) return;

        tray.MouseClick += (_, e) => { if (e.Button == MouseButtons.Left) ToggleFlyout(); };
        widget.Clicked += ToggleFlyout;
        widget.Wheeled += steps => { if (S.Cities.Count > 0) Cycle(steps); };
        widget.RightClicked += () =>
        {
            Native.SetForegroundWindow(host.Handle);
            menu.Show(Cursor.Position);
        };

        SystemEvents.PowerModeChanged += (_, e) => { if (e.Mode == PowerModes.Resume) host.BeginInvoke(() => RefreshSoon(TimeSpan.FromSeconds(15))); };
        SystemEvents.DisplaySettingsChanged += (_, _) => host.BeginInvoke(() => { widget.Invalidate(); widget.Ensure(); });
        SystemEvents.UserPreferenceChanged += (_, _) => host.BeginInvoke(widget.Invalidate);

        widget.SetPlacement(S.WidgetSide == "Left", S.WidgetOffset);
        widget.Ensure();
        UpdateWidget();

        timer.Tick += (_, _) => Tick();
        timer.Start();
        _ = RefreshAsync(); // right away, so the widget shows live weather within seconds

        Updater.CleanupOld();
        if (Environment.GetCommandLineArgs().Contains("--updated"))
            tray.ShowBalloonTip(6000, L.T("AppName"), L.F("Updated", Updater.CurrentText), ToolTipIcon.Info);
        updateTimer.Tick += async (_, _) => { updateTimer.Interval = 6 * 3600 * 1000; await CheckForUpdatesAsync(false); };
        updateTimer.Start(); // first check 45 s after start, then every 6 hours
    }

    static Icon LoadIcon()
    {
        using var stream = typeof(SkyGlanceApp).Assembly.GetManifestResourceStream("app.ico");
        return stream != null ? new Icon(stream) : SystemIcons.Application;
    }

    void Tick()
    {
        widget.Ensure(); // handles Explorer restarts and tray width changes
        if (DateTime.UtcNow >= nextFetchUtc && !Fetching) _ = RefreshAsync();
        if (S.ShowExtremes && DateTime.UtcNow >= nextWorldUtc && !fetchingWorld) _ = RefreshWorldAsync();
        UpdateWidget(); // the current hour moves on even between fetches
    }

    void RefreshSoon(TimeSpan delay)
    {
        var at = DateTime.UtcNow + delay;
        if (at < nextFetchUtc) nextFetchUtc = at;
    }

    /// <summary>Finds my location if needed, then fetches the forecast of the place shown. Returns false when it couldn't.</summary>
    public async Task<bool> RefreshAsync()
    {
        if (Headless || Fetching) return false;
        Fetching = true;
        Changed?.Invoke();
        try
        {
            // Auto location: detect at first run and again every 6 hours (a laptop moves).
            if (S.AutoLocation && (!S.HasLocation || (DateTime.UtcNow - S.LocationUpdatedUtc).TotalHours >= 6))
            {
                var loc = await LocationService.DetectAsync(S.Language);
                if (loc != null) SetLocation(S, loc);
                S.Save();
            }
            if (ActivePlace is not { } place) { LastFetchFailed = false; nextFetchUtc = DateTime.UtcNow.AddMinutes(2); return false; }

            // Stored under the coordinates asked for: Open-Meteo snaps them to its grid in the answer.
            var f = await WeatherService.FetchAsync(place.Latitude, place.Longitude);
            forecasts[place.Key] = f;
            LastFetchFailed = false;
            failures = 0;
            nextFetchUtc = DateTime.UtcNow.AddMinutes(Math.Clamp(S.RefreshMinutes, 5, 180));
            SaveForecasts();
            return true;
        }
        catch (Exception ex)
        {
            Log.Error(ex);
            LastFetchFailed = true;
            // Back off: 1, 2, 4... up to 15 minutes while offline.
            failures++;
            nextFetchUtc = DateTime.UtcNow.AddMinutes(Math.Min(15, 1 << Math.Min(4, failures - 1)));
            return false;
        }
        finally
        {
            Fetching = false;
            UpdateWidget();
            Changed?.Invoke();
        }
    }

    /// <summary>Fetches the world extremes; every hour, or after 10 minutes when it failed.</summary>
    async Task RefreshWorldAsync()
    {
        if (Headless) return;
        fetchingWorld = true;
        try
        {
            World = await WeatherService.FetchExtremesAsync() ?? World;
            nextWorldUtc = DateTime.UtcNow.AddHours(1);
        }
        catch (Exception ex)
        {
            Log.Error(ex);
            nextWorldUtc = DateTime.UtcNow.AddMinutes(10);
        }
        finally
        {
            fetchingWorld = false;
            Changed?.Invoke();
        }
    }

    /// <summary>Saves the cache, dropping places that are no longer my location or a saved city.</summary>
    void SaveForecasts()
    {
        var keep = S.Cities.Select(c => c.Key).ToHashSet();
        if (S.HasLocation) keep.Add(Place.KeyOf(S.Latitude, S.Longitude));
        foreach (var k in forecasts.Keys.Where(k => !keep.Contains(k)).ToList()) forecasts.Remove(k);
        ForecastStore.Save(forecasts);
    }

    /// <summary>Shows saved city <paramref name="index"/> (-1 = my location) and fetches it if its forecast is old.</summary>
    public void SwitchTo(int index)
    {
        index = CityCycle.Clamp(index, S.Cities.Count);
        if (index == S.ActiveCity) return;
        S.ActiveCity = index;
        if (!Headless) S.Save();
        LastFetchFailed = false;
        UpdateWidget();
        Changed?.Invoke();
        if (Forecast == null || (DateTime.UtcNow - Forecast.FetchedUtc).TotalMinutes >= Math.Min(S.RefreshMinutes, 30))
            nextFetchUtc = DateTime.UtcNow; // the next tick fetches; a fetch running now for the old city finishes first
    }

    /// <summary>Moves to the next (+1) or previous (-1) place.</summary>
    public void Cycle(int steps) => SwitchTo(CityCycle.Step(S.ActiveCity, S.Cities.Count, steps));

    /// <summary>Display name of place <paramref name="index"/> (-1 = my location).</summary>
    public string PlaceName(int index) => index >= 0 && index < S.Cities.Count ? S.Cities[index].Name
        : S.HasLocation ? S.LocationName : L.T("MyLocation");

    public static void SetLocation(AppSettings s, LocResult loc)
    {
        s.HasLocation = true;
        s.Latitude = loc.Lat;
        s.Longitude = loc.Lon;
        s.LocationName = loc.Name;
        s.LocationUpdatedUtc = DateTime.UtcNow;
    }

    // ---------- what to show ----------

    public DateTime LocalNow => Forecast?.LocalNow(DateTime.UtcNow) ?? DateTime.Now;

    /// <summary>The conditions shown now: the current reading, or the matching hour of an older forecast.</summary>
    public (double tempC, int code, bool day)? Now()
    {
        if (Forecast is not { } f) return null;
        if ((DateTime.UtcNow - f.FetchedUtc).TotalMinutes < 75) return (f.Current.TemperatureC, f.Current.Code, f.Current.IsDay);
        var h = f.HoursFrom(f.LocalNow(DateTime.UtcNow), 1).FirstOrDefault();
        return h == null ? null : (h.TemperatureC, h.Code, h.IsDay);
    }

    /// <summary>Today's high and low at the location, such as "↑22° ↓13°", or null before the first forecast.</summary>
    public string? HighLow()
    {
        if (Forecast?.Today(LocalNow) is not { } d) return null;
        return $"\u2191{UnitFormat.Temperature(d.MaxC, S.Temperature)} \u2193{UnitFormat.Temperature(d.MinC, S.Temperature)}";
    }

    void UpdateWidget()
    {
        if (Headless) return;
        var now = Now();
        string t = now is { } n ? UnitFormat.Temperature(n.tempC, S.Temperature) : "--";
        string cond = now is { } m ? L.Condition(m.code) : !S.HasLocation ? L.T("Location") : "";
        widget.SetContent(t, cond, S.ShowHighLow ? HighLow() ?? "" : null, now is { } k ? WeatherCode.Icon(k.code) : SkyIcon.Cloudy, now?.day ?? true, S.ShowCondition, L.Ar);
        string tip = now is { } p
            ? $"{L.T("AppName")}: {ActivePlace?.Name}\n{UnitFormat.Temperature(p.tempC, S.Temperature)} {L.Condition(p.code)}"
            : $"{L.T("AppName")}\n{(S.HasLocation ? L.T("Loading") : L.T("NoLocation"))}";
        tray.Text = tip.Length <= 127 ? tip : tip[..127];
    }

    // ---------- windows ----------

    void ToggleFlyout()
    {
        if (flyout?.Visible == true) { flyout.Hide(); return; }
        // A click on the widget first deactivates (hides) the flyout; don't reopen it on that same click.
        if (flyout != null && (DateTime.Now - flyout.LastHidden).TotalMilliseconds < 300) return;
        ShowFlyout();
    }

    void ShowFlyout()
    {
        flyout ??= new WeatherFlyout(this);
        var anchor = widget.ScreenBounds ?? new Rectangle(Cursor.Position, new Size(1, 1));
        flyout.ShowNear(anchor);
        // Opening the flyout on an old forecast asks for a fresh one.
        if (Forecast == null || (DateTime.UtcNow - Forecast.FetchedUtc).TotalMinutes >= 10) _ = RefreshAsync();
    }

    DayForm? dayForm;

    /// <summary>Opens "weather on a date", on <paramref name="date"/> or today.</summary>
    public void ShowCalendar(DateOnly? date = null)
    {
        flyout?.Hide();
        if (dayForm is { IsDisposed: false })
        {
            if (date is { } d) dayForm.ShowDate(d);
            dayForm.Activate();
            return;
        }
        dayForm = new DayForm(this, date);
        dayForm.Show();
        dayForm.Activate();
    }

    public void ShowSettings(int page = 0)
    {
        flyout?.Hide();
        if (settingsForm is { IsDisposed: false }) { settingsForm.ShowPage(page); settingsForm.Activate(); return; }
        settingsForm = new SettingsForm(this);
        settingsForm.ShowPage(page);
        settingsForm.Show();
        settingsForm.Activate();
    }

    public void ApplySettings(AppSettings s)
    {
        bool langChanged = s.Language != S.Language;
        s.ActiveCity = CityCycle.Clamp(s.ActiveCity, s.Cities.Count);
        string? before = ActivePlace?.Key;
        bool redetect = s.AutoLocation && !S.AutoLocation;
        if (s.AutoLocation && !S.AutoLocation) s.LocationUpdatedUtc = DateTime.MinValue; // detect again now
        S = s;
        L.Ar = S.Language == "ar";
        S.Save();
        widget.SetPlacement(S.WidgetSide == "Left", S.WidgetOffset);
        if (langChanged)
        {
            flyout?.Dispose();
            flyout = null;
            if (dayForm is { IsDisposed: false }) dayForm.Close();
        }
        if (redetect || ActivePlace?.Key != before || Forecast == null)
        {
            LastFetchFailed = false;
            nextFetchUtc = DateTime.UtcNow;
            if (!Fetching) _ = RefreshAsync();
        }
        else if (DateTime.UtcNow.AddMinutes(S.RefreshMinutes) < nextFetchUtc) nextFetchUtc = DateTime.UtcNow.AddMinutes(S.RefreshMinutes);
        UpdateWidget();
        widget.Invalidate();
        Changed?.Invoke();
    }

    // ---------- updates ----------

    bool installing;

    /// <summary>Checks GitHub; installs automatically when allowed. Returns a status line.</summary>
    public async Task<string> CheckForUpdatesAsync(bool userInitiated)
    {
        if (!userInitiated && !S.AutoCheckUpdates) return "";
        Updater.Release? rel;
        try { rel = await Updater.CheckAsync(); }
        catch (Exception ex)
        {
            Log.Error(ex);
            return L.T("UpdateCheckFailed");
        }
        S.LastUpdateCheckUtc = DateTime.UtcNow;
        S.Save();
        if (rel == null) return L.F("UpToDate", Updater.CurrentText);

        string ver = rel.Version.ToString(3);
        if (userInitiated || S.AutoInstallUpdates)
        {
            _ = InstallUpdateAsync(rel);
            return L.F("Updating", ver);
        }
        tray.ShowBalloonTip(10000, L.T("AppName"), L.F("UpdateAvailable", ver), ToolTipIcon.Info);
        return L.F("UpdateAvailable", ver);
    }

    async Task InstallUpdateAsync(Updater.Release rel)
    {
        if (installing) return;
        installing = true;
        tray.ShowBalloonTip(4000, L.T("AppName"), L.F("Updating", rel.Version.ToString(3)), ToolTipIcon.Info);
        if (await Updater.InstallAsync(rel))
        {
            Exit();
            return;
        }
        installing = false;
        tray.ShowBalloonTip(8000, L.T("AppName"), L.T("UpdateFailed"), ToolTipIcon.Warning);
        Updater.OpenPage(rel.PageUrl);
    }

    void BuildMenu()
    {
        menu.Items.Clear();
        menu.RightToLeft = L.Ar ? RightToLeft.Yes : RightToLeft.No;
        menu.Items.Add(L.T("ShowWeather"), null, (_, _) => ShowFlyout()).Font = new Font(menu.Font, FontStyle.Bold);
        menu.Items.Add(L.T("RefreshNow"), null, async (_, _) => await RefreshAsync());
        menu.Items.Add(L.T("WeatherOnDate") + "...", null, (_, _) => ShowCalendar());
        if (S.Cities.Count > 0)
        {
            var cityMenu = new ToolStripMenuItem(L.T("City"));
            for (int i = -1; i < S.Cities.Count; i++)
            {
                int index = i;
                var item = new ToolStripMenuItem(i < 0 ? $"{PlaceName(-1)} ({L.T("MyLocation")})" : PlaceName(i)) { Checked = CityCycle.Clamp(S.ActiveCity, S.Cities.Count) == i };
                item.Click += (_, _) => SwitchTo(index);
                cityMenu.DropDownItems.Add(item);
            }
            cityMenu.DropDownItems.Add(new ToolStripSeparator());
            cityMenu.DropDownItems.Add(L.T("ManageCities"), null, (_, _) => ShowSettings(SettingsForm.LocationPage));
            menu.Items.Add(cityMenu);
        }
        else
            menu.Items.Add(L.T("ChangeLocation"), null, (_, _) => ShowSettings(SettingsForm.LocationPage));

        var unitMenu = new ToolStripMenuItem(L.T("TempUnitMenu"));
        foreach (var (u, key) in new[] { (TemperatureUnit.Celsius, "Celsius"), (TemperatureUnit.Fahrenheit, "Fahrenheit") })
        {
            var item = new ToolStripMenuItem(L.T(key)) { Checked = S.Temperature == u };
            item.Click += (_, _) => { var s = S.Clone(); s.Temperature = u; ApplySettings(s); };
            unitMenu.DropDownItems.Add(item);
        }
        menu.Items.Add(unitMenu);

        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(L.T("Settings"), null, (_, _) => ShowSettings());
        menu.Items.Add(L.T("CheckForUpdates"), null, async (_, _) =>
        {
            var msg = await CheckForUpdatesAsync(userInitiated: true);
            if (!installing) tray.ShowBalloonTip(6000, L.T("AppName"), msg, ToolTipIcon.Info);
        });
        var startup = new ToolStripMenuItem(L.T("StartWithWindows")) { Checked = AppSettings.StartWithWindows };
        startup.Click += (_, _) => { try { AppSettings.StartWithWindows = !startup.Checked; } catch { } };
        menu.Items.Add(startup);
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(L.T("Exit"), null, (_, _) => Exit());
    }

    public const string ShowFlyoutMessage = "SkyGlance.ShowFlyout";

    public void OnTaskbarCreated() => widget.Ensure();

    public void ShowFlyoutFromOutside()
    {
        if (flyout?.Visible == true) return;
        ShowFlyout();
    }

    public void Exit()
    {
        timer.Stop();
        tray.Visible = false;
        tray.Dispose();
        if (widget.Handle != IntPtr.Zero) widget.DestroyHandle();
        ExitThread();
    }
}
