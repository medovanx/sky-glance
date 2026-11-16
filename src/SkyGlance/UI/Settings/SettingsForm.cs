namespace SkyGlance.UI.Settings;

/// <summary>Settings in the Windows 11 Settings style: a sidebar of pages, each a rounded card.</summary>
sealed class SettingsForm : Form
{
    public const int GeneralPage = 0, LocationPage = 1, UnitsPage = 2, UpdatesPage = 3;

    readonly SkyGlanceApp app;
    readonly AppSettings s;
    readonly Theme t = Theme.Current;

    // General
    readonly ThemedCombo cmbLang = Combo(), cmbSide = Combo(), cmbRefresh = Combo();
    readonly ThemedNumber numOffset = Num(-2000, 2000);
    readonly ToggleSwitch tglStartup = new(), tglCondition = new(), tglHighLow = new(), tglExtremes = new(), tglSeason = new();
    static readonly int[] RefreshOptions = [10, 15, 30, 60];
    // Location
    readonly ToggleSwitch tglAuto = new();
    readonly Label lblPlace = new() { AutoSize = true, Font = new Font("Segoe UI Semibold", 11f), MaximumSize = new Size(490, 0), Margin = new Padding(0, 0, 0, 6) };
    readonly Label lblLocStatus = new() { AutoSize = true, Tag = "dim", MaximumSize = new Size(490, 0), Margin = new Padding(0, 4, 0, 4) };
    readonly Button btnDetect = new() { AutoSize = true, Height = 30 };
    readonly ThemedText txtSearch = new() { Width = 392 };
    readonly Button btnSearch = new() { Size = new Size(90, 30) }, btnUse = new() { AutoSize = true, Height = 30 }, btnAddCity = new() { AutoSize = true, Height = 30 };
    readonly FlowLayoutPanel citiesPanel = new() { FlowDirection = FlowDirection.TopDown, WrapContents = false, AutoSize = true, Margin = Padding.Empty };
    readonly Label lblNoCities = new() { AutoSize = true, Tag = "dim", MaximumSize = new Size(490, 0), Margin = new Padding(0, 0, 0, 6) };
    // Same left edge as the rows above and below it (a control's default margin is 3 px), and the same width as the search row.
    readonly ThemedCombo cmbResults = new() { Width = 490, Margin = new Padding(0, 4, 0, 4) };
    readonly List<LocResult> results = [];
    // Units
    readonly ThemedCombo cmbTemp = Combo(260), cmbWind = Combo(260), cmbPrecip = Combo(260);
    // Updates
    readonly ToggleSwitch tglAutoCheck = new(), tglAutoInstall = new();
    readonly Button btnCheck = new() { AutoSize = true, Height = 30 };
    readonly Label lblUpdate = new() { AutoSize = true, Tag = "dim", MaximumSize = new Size(440, 0) };

    readonly Button btnSave = new() { Size = new Size(110, 34) }, btnCancel = new() { Size = new Size(110, 34) };
    readonly List<(string glyph, string title, Control page)> pages = [];
    readonly NavList nav;
    readonly Panel content = new() { Dock = DockStyle.Fill, Padding = new Padding(20, 20, 0, 0) };

    public SettingsForm(SkyGlanceApp app)
    {
        this.app = app;
        s = app.S.Clone();
        Text = $"{L.T("Settings")} - {L.T("AppName")}";
        Icon = app.AppIcon;
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;
        AutoScaleDimensions = new SizeF(96f, 96f);
        AutoScaleMode = AutoScaleMode.Dpi;
        ClientSize = new Size(800, 560);
        Font = new Font("Segoe UI", 9.75f);
        BackColor = t.Window;
        ForeColor = t.Text;
        if (L.Ar) RightToLeft = RightToLeft.Yes;

        BuildGeneral();
        BuildLocation();
        BuildUnits();
        BuildUpdates();

        nav = new NavList(pages.Select(p => (p.glyph, p.title)).ToArray(), t) { Dock = L.Ar ? DockStyle.Right : DockStyle.Left, Width = 210 };
        nav.SelectedChanged += ShowPage;

        var footer = new Panel { Dock = DockStyle.Bottom, Height = 62, Padding = new Padding(20, 12, 20, 16) };
        btnSave.Text = L.T("Save");
        btnCancel.Text = L.T("Cancel");
        btnSave.Click += (_, _) => Save();
        btnCancel.Click += (_, _) => Close();
        CancelButton = btnCancel;
        var buttons = new FlowLayoutPanel { Dock = L.Ar ? DockStyle.Left : DockStyle.Right, AutoSize = true, FlowDirection = L.Ar ? FlowDirection.LeftToRight : FlowDirection.RightToLeft, WrapContents = false };
        buttons.Controls.AddRange([btnSave, btnCancel]);
        footer.Controls.Add(buttons);

        foreach (var p in pages) { p.page.Visible = false; content.Controls.Add(p.page); }
        Controls.Add(content);
        Controls.Add(footer);
        Controls.Add(nav);

        Styler.Apply(this, t);
        Styler.Primary(btnSave, t);
        ShowPage(0);
        SyncEnabled();
    }

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        Native.RoundCorners(Handle, t.Dark);
        int caption = ColorTranslator.ToWin32(t.Window);
        Native.DwmSetWindowAttribute(Handle, 35, ref caption, sizeof(int)); // DWMWA_CAPTION_COLOR
    }

    public int PageCount => pages.Count;

    public void ShowPage(int i)
    {
        i = Math.Clamp(i, 0, pages.Count - 1);
        for (int k = 0; k < pages.Count; k++) pages[k].page.Visible = k == i;
        nav.Selected = i;
        if (i == LocationPage && !s.AutoLocation) BeginInvoke(() => txtSearch.Focus());
    }

    // ---------- pages ----------

    void AddPage(string glyph, string title, params Control[] sections)
    {
        var flow = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, WrapContents = false, AutoScroll = true };
        var heading = new Label { Text = title, AutoSize = true, Font = new Font("Segoe UI Semibold", 17f), Margin = new Padding(2, 0, 0, 12) };
        flow.Controls.Add(heading);
        foreach (var sec in sections) { sec.Margin = new Padding(0, 0, 0, 12); flow.Controls.Add(sec); }
        Native.DarkScrollbars(flow, t.Dark);
        pages.Add((glyph, title, flow));
    }

    void BuildGeneral()
    {
        cmbLang.Items.AddRange(["English", "العربية"]);
        cmbLang.SelectedIndex = s.Language == "ar" ? 1 : 0;
        cmbSide.Items.AddRange([L.T("SideRight"), L.T("SideLeft")]);
        cmbSide.SelectedIndex = s.WidgetSide == "Left" ? 1 : 0;
        numOffset.Value = Math.Clamp(s.WidgetOffset, -2000, 2000);
        numOffset.Increment = 10;
        foreach (var m in RefreshOptions) cmbRefresh.Items.Add(L.F("Minutes", m));
        int ri = Array.IndexOf(RefreshOptions, s.RefreshMinutes);
        cmbRefresh.SelectedIndex = ri >= 0 ? ri : 1;
        tglCondition.Checked = s.ShowCondition;
        tglHighLow.Checked = s.ShowHighLow;
        tglExtremes.Checked = s.ShowExtremes;
        tglSeason.Checked = s.ShowSeason;
        tglStartup.Checked = !app.Headless && AppSettings.StartWithWindows;
        AddPage(Art.IconGeneral, L.T("General"),
            Card(L.T("General"), Art.IconGeneral,
                Row(L.T("Language"), cmbLang),
                Row(L.T("RefreshEvery"), cmbRefresh),
                Row(L.T("StartWithWindows"), tglStartup)),
            Card(L.T("Taskbar"), Art.IconTaskbar,
                Row(L.T("ShowCondition"), tglCondition),
                Row(L.T("ShowHighLow"), tglHighLow),
                Row(L.T("WidgetPosition"), cmbSide),
                Row(L.T("WidgetOffset"), numOffset)),
            Card(L.T("Flyout"), Art.IconWeather,
                Row(L.T("ShowSeason"), tglSeason),
                Row(L.T("ShowExtremes"), tglExtremes)));
    }

    void BuildLocation()
    {
        tglAuto.Checked = s.AutoLocation;
        lblPlace.Text = s.HasLocation ? s.LocationName : L.T("NotSet");
        lblPlace.Tag = "keep";
        lblPlace.ForeColor = t.Text;
        btnDetect.Text = L.T("DetectNow");
        btnSearch.Text = L.T("Search");
        btnUse.Text = L.T("UseCity");
        btnAddCity.Text = L.T("AddCity");
        lblNoCities.Text = L.T("NoCities");
        txtSearch.PlaceholderText = L.T("SearchHint");
        txtSearch.RightToLeft = RightToLeft;
        tglAuto.CheckedChanged += (_, _) => SyncEnabled();
        txtSearch.KeyDown += (_, e) => { if (e.KeyCode == Keys.Enter) { e.SuppressKeyPress = true; _ = SearchAsync(); } };
        btnSearch.Click += async (_, _) => await SearchAsync();
        btnUse.Click += (_, _) => UseResult();
        btnAddCity.Click += (_, _) => AddResult();
        btnDetect.Click += async (_, _) =>
        {
            btnDetect.Enabled = false;
            lblLocStatus.Text = L.T("Detecting");
            var loc = app.Headless ? null : await LocationService.DetectAsync(cmbLang.SelectedIndex == 1 ? "ar" : "en");
            btnDetect.Enabled = true;
            if (loc == null) { lblLocStatus.Text = L.T("DetectFailed"); return; }
            SkyGlanceApp.SetLocation(s, loc);
            lblPlace.Text = loc.Name;
            lblLocStatus.Text = L.F("CityChosen", loc.Name);
        };
        var autoNote = new Label { Text = L.T("AutoLocationNote"), AutoSize = true, MaximumSize = new Size(490, 0), Tag = "dim", Margin = new Padding(0, 0, 0, 6) };
        var privacy = new Label { Text = L.T("PrivacyNote"), AutoSize = true, MaximumSize = new Size(490, 0), Tag = "dim", Margin = new Padding(0, 8, 0, 4) };
        if (app.Headless)
        {
            // Screenshots show a made-up search, never a real one.
            txtSearch.Text = "Lisbon";
            results.Add(new LocResult(38.72, -9.14, L.Ar ? "لشبونة، البرتغال" : "Lisbon, Portugal"));
            results.Add(new LocResult(37.73, -97.4, L.Ar ? "لشبونة، أوهايو، الولايات المتحدة" : "Lisbon, Ohio, United States"));
            foreach (var r in results) cmbResults.Items.Add(r.Name);
            cmbResults.SelectedIndex = 0;
        }
        FillCities();
        var citiesNote = new Label { Text = L.T("CitiesNote"), AutoSize = true, MaximumSize = new Size(490, 0), Tag = "dim", Margin = new Padding(0, 6, 0, 4) };
        AddPage(Art.IconLocation, L.T("Location"),
            Card(L.T("CurrentLocation"), Art.IconLocation, lblPlace, Row(L.T("AutoLocation"), tglAuto), autoNote, Flow(btnDetect)),
            Card(L.T("SearchCity"), Art.IconSearch, Flow(txtSearch, btnSearch), cmbResults, Flow(btnUse, btnAddCity), lblLocStatus, privacy),
            Card(L.T("MyCities"), Art.IconWeather, lblNoCities, citiesPanel, citiesNote));
    }

    async Task SearchAsync()
    {
        string q = txtSearch.Text.Trim();
        if (q.Length < 2) return;
        btnSearch.Enabled = false;
        lblLocStatus.Text = L.T("Searching");
        var found = await LocationService.SearchAsync(q, cmbLang.SelectedIndex == 1 ? "ar" : "en");
        btnSearch.Enabled = true;
        results.Clear();
        results.AddRange(found);
        cmbResults.Items.Clear();
        foreach (var r in results) cmbResults.Items.Add(r.Name);
        if (results.Count > 0) cmbResults.SelectedIndex = 0;
        lblLocStatus.Text = results.Count == 0 ? L.T("NoResults") : "";
        SyncEnabled();
    }

    void UseResult()
    {
        if (cmbResults.SelectedIndex < 0 || cmbResults.SelectedIndex >= results.Count) return;
        var r = results[cmbResults.SelectedIndex];
        SkyGlanceApp.SetLocation(s, r);
        tglAuto.Checked = false; // a chosen city stays put
        lblPlace.Text = r.Name;
        lblLocStatus.Text = L.F("CityChosen", r.Name);
        SyncEnabled();
    }

    void AddResult()
    {
        if (cmbResults.SelectedIndex < 0 || cmbResults.SelectedIndex >= results.Count) return;
        var r = results[cmbResults.SelectedIndex];
        var place = new Place(r.Name, r.Lat, r.Lon);
        if (s.Cities.Any(c => c.Key == place.Key)) { lblLocStatus.Text = L.F("CityAlready", r.Name); return; }
        s.Cities.Add(place);
        lblLocStatus.Text = L.F("CityAdded", r.Name);
        FillCities();
    }

    /// <summary>One row per saved city: its name and a remove button.</summary>
    void FillCities()
    {
        citiesPanel.SuspendLayout();
        foreach (Control c in citiesPanel.Controls.Cast<Control>().ToList()) { citiesPanel.Controls.Remove(c); c.Dispose(); }
        for (int i = 0; i < s.Cities.Count; i++)
        {
            int index = i;
            var remove = new Button { Text = L.T("Remove"), AutoSize = true, Height = 28 };
            remove.Click += (_, _) =>
            {
                s.ActiveCity = CityCycle.AfterRemove(s.ActiveCity, index);
                s.Cities.RemoveAt(index);
                BeginInvoke(FillCities); // not while this button's own click is running
            };
            var row = Row(s.Cities[i].Name, remove);
            citiesPanel.Controls.Add(row);
        }
        lblNoCities.Visible = s.Cities.Count == 0;
        Styler.Apply(citiesPanel, t);
        citiesPanel.ResumeLayout();
    }

    void BuildUnits()
    {
        cmbTemp.Items.AddRange([L.T("Celsius"), L.T("Fahrenheit")]);
        cmbTemp.SelectedIndex = (int)s.Temperature;
        cmbWind.Items.AddRange([L.T("WindKmh"), L.T("WindMs"), L.T("WindMph"), L.T("WindKn"), L.T("WindBft")]);
        cmbWind.SelectedIndex = (int)s.Wind;
        cmbPrecip.Items.AddRange([L.T("PrecipMm"), L.T("PrecipIn")]);
        cmbPrecip.SelectedIndex = (int)s.Precipitation;
        AddPage(Art.IconRuler, L.T("Units"), Card(L.T("Units"), Art.IconRuler,
            Row(L.T("TempUnit"), cmbTemp),
            Row(L.T("WindUnit"), cmbWind),
            Row(L.T("PrecipUnit"), cmbPrecip)));
    }

    void BuildUpdates()
    {
        tglAutoCheck.Checked = s.AutoCheckUpdates;
        tglAutoInstall.Checked = s.AutoInstallUpdates;
        btnCheck.Text = L.T("CheckNow");
        var version = new Label { Text = L.F("Version", Updater.CurrentText), AutoSize = true, Font = new Font("Segoe UI Semibold", 11f), Margin = new Padding(0, 0, 0, 8) };
        AddPage(Art.IconUpdate, L.T("Updates"), Card(L.T("Updates"), Art.IconUpdate,
            version,
            Row(L.T("AutoCheck"), tglAutoCheck),
            Row(L.T("AutoInstall"), tglAutoInstall),
            Flow(btnCheck, lblUpdate)));
        tglAutoCheck.CheckedChanged += (_, _) => SyncEnabled();
        btnCheck.Click += async (_, _) =>
        {
            btnCheck.Enabled = false;
            lblUpdate.Text = "...";
            lblUpdate.Text = await app.CheckForUpdatesAsync(userInitiated: true);
            btnCheck.Enabled = true;
        };
    }

    void SyncEnabled()
    {
        tglAutoInstall.Enabled = tglAutoCheck.Checked;
        btnDetect.Enabled = tglAuto.Checked;
        btnUse.Enabled = btnAddCity.Enabled = results.Count > 0;
        cmbResults.Enabled = results.Count > 0;
    }

    void Save()
    {
        s.Language = cmbLang.SelectedIndex == 1 ? "ar" : "en";
        s.WidgetSide = cmbSide.SelectedIndex == 1 ? "Left" : "Right";
        s.WidgetOffset = (int)numOffset.Value;
        s.RefreshMinutes = RefreshOptions[Math.Max(0, cmbRefresh.SelectedIndex)];
        s.ShowCondition = tglCondition.Checked;
        s.ShowHighLow = tglHighLow.Checked;
        s.ShowExtremes = tglExtremes.Checked;
        s.ShowSeason = tglSeason.Checked;
        s.AutoLocation = tglAuto.Checked;
        // The city may have been switched from the flyout while this window was open: keep that unless the list changed.
        if (s.Cities.Select(c => c.Key).SequenceEqual(app.S.Cities.Select(c => c.Key))) s.ActiveCity = app.S.ActiveCity;
        s.Temperature = (TemperatureUnit)Math.Max(0, cmbTemp.SelectedIndex);
        s.Wind = (WindUnit)Math.Max(0, cmbWind.SelectedIndex);
        s.Precipitation = (PrecipitationUnit)Math.Max(0, cmbPrecip.SelectedIndex);
        s.AutoCheckUpdates = tglAutoCheck.Checked;
        s.AutoInstallUpdates = tglAutoInstall.Checked;
        try { AppSettings.StartWithWindows = tglStartup.Checked; } catch { /* registry blocked */ }
        app.ApplySettings(s);
        Close();
    }

    // ---------- layout helpers ----------

    static ThemedCombo Combo(int width = 220) => new() { Width = width };
    static ThemedNumber Num(int min, int max) => new() { Minimum = min, Maximum = max, Width = 120 };

    Section Card(string title, string glyph, params Control[] rows)
    {
        var sec = new Section(title) { Glyph = glyph, Theme = t, RightToLeft = RightToLeft };
        sec.MinimumSize = new Size(526, 0);
        sec.MaximumSize = new Size(526, 0);
        var stack = new FlowLayoutPanel { FlowDirection = FlowDirection.TopDown, WrapContents = false, AutoSize = true, Location = new Point(16, 44), MinimumSize = new Size(494, 0), MaximumSize = new Size(494, 0), RightToLeft = RightToLeft };
        foreach (var r in rows) stack.Controls.Add(r);
        sec.Controls.Add(stack);
        return sec;
    }

    /// <summary>Label on the near side, control on the far side.</summary>
    static TableLayoutPanel Row(string label, Control c)
    {
        var tl = new TableLayoutPanel { ColumnCount = 2, RowCount = 1, Width = 494, Height = Math.Max(36, (c.AutoSize ? c.PreferredSize.Height : c.Height) + 8), Margin = new Padding(0) };
        tl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        tl.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        tl.Controls.Add(new Label { Text = label, AutoSize = true, Anchor = AnchorStyles.Left, MaximumSize = new Size(300, 0) }, 0, 0);
        c.Anchor = AnchorStyles.Right;
        c.Margin = new Padding(0, c.Margin.Top, 0, c.Margin.Bottom);
        tl.Controls.Add(c, 1, 0);
        return tl;
    }

    /// <summary>Label above a wide control.</summary>
    static FlowLayoutPanel Stacked(string label, Control c)
    {
        var f = new FlowLayoutPanel { FlowDirection = FlowDirection.TopDown, AutoSize = true, WrapContents = false, Margin = new Padding(0, 4, 0, 8) };
        f.Controls.Add(new Label { Text = label, AutoSize = true, Margin = new Padding(0, 4, 0, 4) });
        if (c is ThemedCombo) c.Width = 490;
        f.Controls.Add(c);
        return f;
    }

    static FlowLayoutPanel Flow(params Control[] cs)
    {
        var p = new FlowLayoutPanel { AutoSize = true, WrapContents = false, Margin = new Padding(0, 4, 0, 4) };
        int tallest = cs.Max(c => c.Height);
        for (int i = 0; i < cs.Length; i++)
        {
            int top = (tallest - cs[i].Height) / 2;
            cs[i].Margin = new Padding(0, top, i < cs.Length - 1 ? 8 : 0, 0);
            p.Controls.Add(cs[i]);
        }
        return p;
    }
}
