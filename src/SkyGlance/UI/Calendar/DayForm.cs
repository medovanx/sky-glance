namespace SkyGlance.UI.Calendar;

/// <summary>
/// "Weather on a date": a month calendar and a city list on one side, the weather of the picked day on the other.
/// Any day from 1 January 1940 up to 15 days ahead.
/// </summary>
sealed class DayForm : Form
{
    readonly SkyGlanceApp app;
    readonly Theme t = Theme.Current;
    readonly DayPicker picker = new() { Size = new Size(300, 300) };
    readonly ThemedCombo cmbPlace = new() { Width = 300 };
    readonly DayView view;
    readonly List<Place> places = [];
    readonly Dictionary<string, DayReport> cache = [];
    int request;

    public DayForm(SkyGlanceApp app, DateOnly? date = null)
    {
        this.app = app;
        Text = $"{L.T("WeatherOnDate")} - {L.T("AppName")}";
        Icon = app.AppIcon;
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;
        AutoScaleDimensions = new SizeF(96f, 96f);
        AutoScaleMode = AutoScaleMode.Dpi;
        ClientSize = new Size(800, 452);
        Font = new Font("Segoe UI", 9.75f);
        BackColor = t.Window;
        ForeColor = t.Text;
        KeyPreview = true;
        if (L.Ar) RightToLeft = RightToLeft.Yes;

        view = new DayView(app) { Theme = t, RightToLeft = RightToLeft, Bounds = new Rectangle(L.Ar ? 20 : 340, 20, 440, 412) };
        var side = new Panel { Bounds = new Rectangle(L.Ar ? 480 : 20, 20, 300, 412), RightToLeft = RightToLeft };
        var placeLabel = new Label { Text = L.T("Location"), AutoSize = true, Location = new Point(0, 0) };
        cmbPlace.Location = new Point(0, 24);
        picker.Location = new Point(0, 68);
        var note = new Label { Text = L.T("DateRangeNote"), AutoSize = true, MaximumSize = new Size(300, 0), Tag = "dim", Location = new Point(0, 376) };
        side.Controls.AddRange([placeLabel, cmbPlace, picker, note]);
        Controls.AddRange([side, view]);

        // My location first, then the saved cities; the one shown in the taskbar is preselected.
        if (app.S.HasLocation) places.Add(new Place(app.S.LocationName, app.S.Latitude, app.S.Longitude));
        places.AddRange(app.S.Cities);
        foreach (var p in places) cmbPlace.Items.Add(p.Name);
        int active = app.ActivePlace is { } ap ? places.FindIndex(p => p.Key == ap.Key) : -1;
        cmbPlace.SelectedIndex = Math.Max(0, active);

        var today = DateOnly.FromDateTime(app.LocalNow);
        picker.Today = today;
        picker.MinDate = DayQuery.FirstDay;
        picker.MaxDate = DayQuery.LastDay(today);
        picker.RightToLeft = RightToLeft;
        picker.Selected = date ?? today;
        picker.DatePicked += picked => _ = LoadAsync();
        cmbPlace.SelectedIndexChanged += (_, _) => _ = LoadAsync();
        KeyDown += (_, e) => { if (e.KeyCode == Keys.Escape) Close(); };

        Styler.Apply(this, t);
        picker.Theme = t;
        Shown += (_, _) => { picker.Focus(); _ = LoadAsync(); };
    }

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        Native.RoundCorners(Handle, t.Dark);
        int caption = ColorTranslator.ToWin32(t.Window);
        Native.DwmSetWindowAttribute(Handle, 35, ref caption, sizeof(int)); // DWMWA_CAPTION_COLOR
    }

    public void ShowDate(DateOnly d) { picker.Selected = d; _ = LoadAsync(); }

    /// <summary>For --shots: shows a made-up report without going online.</summary>
    public void ShowDemo(DayReport r, string place)
    {
        picker.Selected = r.Date;
        view.Date = r.Date;
        view.PlaceName = place;
        view.Show(r);
    }

    async Task LoadAsync()
    {
        if (places.Count == 0) { view.ShowMessage(L.T("NoLocation")); return; }
        if (app.Headless) return;
        var place = places[Math.Max(0, cmbPlace.SelectedIndex)];
        var date = picker.Selected;
        var source = DayQuery.Source(date, picker.Today);
        view.Date = date;
        view.PlaceName = place.Name;
        if (source == DaySource.OutOfRange) { view.ShowMessage(L.T("DateOutOfRange")); return; }
        string key = $"{place.Key}|{date:yyyy-MM-dd}";
        if (cache.TryGetValue(key, out var hit)) { view.Show(hit); return; }

        int mine = ++request; // a later pick wins over a slower earlier answer
        view.ShowMessage(L.T("Loading"));
        try
        {
            var r = await WeatherService.FetchDayAsync(place.Latitude, place.Longitude, date, source);
            // Past days of the forecast are kept: they don't change. Future ones do, so they are fetched each time.
            if (date < picker.Today) cache[key] = r;
            if (mine == request && !IsDisposed) view.Show(r);
        }
        catch (Exception ex)
        {
            Log.Error(ex);
            if (mine == request && !IsDisposed) view.ShowMessage(L.T("DayFailed"));
        }
    }
}
