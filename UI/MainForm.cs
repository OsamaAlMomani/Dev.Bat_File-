namespace AiSetupUi;

/// <summary>
/// Window layout only: controls, their placement and events. Colours come from ThemeManager,
/// tree logic from SetupTree, and the long-running work lives in MainForm.Actions.cs.
/// </summary>
internal sealed partial class MainForm : Form
{
    private readonly AppSettings _settings;
    private readonly SetupTree _setupTree;

    private readonly TreeView _tree = new();
    private readonly TextBox _filter = new();
    private readonly Button _btnClearFilter;
    private readonly ComboBox _themeBox = new();
    private readonly RichTextBox _log = new();
    private readonly ProgressBar _progress = new();
    private readonly Label _stepLabel = new();
    private readonly SplitContainer _split = new();

    // ---- StatusStrip ----
    private readonly StatusStrip _statusStrip = new() { SizingGrip = false };
    private readonly ToolStripStatusLabel _busyLabel = new()
        { BorderSides = ToolStripStatusLabelBorderSides.Right, AutoSize = false, Width = 90,
          TextAlign = ContentAlignment.MiddleLeft, Padding = new Padding(4, 0, 4, 0) };
    private readonly ToolStripStatusLabel _statusLabel = new()
        { Spring = true, TextAlign = ContentAlignment.MiddleLeft, Padding = new Padding(8, 0, 0, 0) };
    private readonly ToolStripStatusLabel _countLabel = new()
        { BorderSides = ToolStripStatusLabelBorderSides.Left, AutoSize = false, Width = 110,
          TextAlign = ContentAlignment.MiddleLeft, Padding = new Padding(8, 0, 4, 0) };

    // ---- Admin banner ----
    private readonly Panel _adminBanner = new() { Dock = DockStyle.Fill, Visible = false };
    private readonly Button _btnAdminBanner;

    // ---- Toolbar buttons ----
    private readonly Button _btnSelectAll;
    private readonly Button _btnSelectNone;
    private readonly Button _btnCheckStatus;
    private readonly Button _btnInstall;
    private readonly Button _btnUninstall;
    private readonly Button _btnScanExtras;
    private readonly Button _btnNews;
    private readonly Button _btnAdmin;
    private readonly Button _btnStop;
    private readonly Button _btnExit;

    private readonly Label _sep1;
    private readonly Label _sep2;
    private readonly Label _sep3;
    private readonly Label _sep4;

    private readonly Logger _logger;
    private readonly List<Button> _lockWhileBusy = new();
    private readonly ToolTip _tips = new() { AutoPopDelay = 8000, InitialDelay = 400, ReshowDelay = 100 };

    public MainForm(AppSettings settings)
    {
        _settings = settings;
        _logger = new Logger(AppendLog);
        _setupTree = new SetupTree(_tree);

        Text = "AI / Data Science / Full-Stack Environment - Setup" + (IsAdmin() ? "  (Administrator)" : "");
        Size = new Size(960, 940);
        MinimumSize = new Size(820, 700);
        if (_settings.WindowWidth > 0 && _settings.WindowHeight > 0)
            Size = new Size(_settings.WindowWidth, _settings.WindowHeight);
        StartPosition = FormStartPosition.CenterScreen;
        Font = new Font("Segoe UI", 9f);
        KeyPreview = true;

        // ---- create buttons ----
        _btnSelectAll = MakeButton("Select All");
        _btnSelectNone = MakeButton("Select None");
        _btnCheckStatus = MakeButton("Check Installed Status");
        _btnInstall = MakeButton("Install Selected", ThemeRole.Primary);
        _btnUninstall = MakeButton("Uninstall Selected", ThemeRole.Danger);
        _btnScanExtras = MakeButton("Scan System Extras");
        _btnNews = MakeButton("Library News & Updates", ThemeRole.Secondary);
        _btnAdmin = MakeButton("Restart as Admin", ThemeRole.Secondary);
        _btnAdminBanner = MakeButton("Restart as Admin", ThemeRole.Primary);
        _btnStop = MakeButton("Stop", ThemeRole.Danger);
        _btnExit = MakeButton("Cancel / Exit");

        _btnStop.Visible = false;
        _btnAdmin.Visible = !IsAdmin();

        _lockWhileBusy.AddRange(new[]
        {
            _btnSelectAll, _btnSelectNone, _btnCheckStatus, _btnInstall,
            _btnUninstall, _btnScanExtras, _btnNews, _btnAdmin, _btnAdminBanner,
        });

        // ---- separators ----
        _sep1 = MakeSeparator();
        _sep2 = MakeSeparator();
        _sep3 = MakeSeparator();
        _sep4 = MakeSeparator();

        // ---- tooltips ----
        _tips.SetToolTip(_btnSelectAll, "Tick every item in the tree (Ctrl+A)");
        _tips.SetToolTip(_btnSelectNone, "Untick every item (Ctrl+D)");
        _tips.SetToolTip(_btnCheckStatus, "Detect what is installed. Nothing is changed. (F5)");
        _tips.SetToolTip(_btnInstall, "Install the checked items (Enter)");
        _tips.SetToolTip(_btnUninstall, "Uninstall / revert the checked items");
        _tips.SetToolTip(_btnScanExtras, "Read installed pip packages not in the catalog (read-only)");
        _tips.SetToolTip(_btnNews, "Search for new libraries, headlines, updates, system info");
        _tips.SetToolTip(_btnAdmin, "Reopen with administrator rights for registry tweaks");
        _tips.SetToolTip(_btnAdminBanner, "Reopen with administrator rights for registry tweaks");
        _tips.SetToolTip(_btnStop, "Cancel the running operation");
        _tips.SetToolTip(_btnExit, "Close the window");

        // ---- handlers ----
        _btnSelectAll.Click += (_, _) => _setupTree.SetAllChecked(true);
        _btnSelectNone.Click += (_, _) => _setupTree.SetAllChecked(false);
        _btnCheckStatus.Click += async (_, _) => await RefreshStatusAsync();
        _btnInstall.Click += async (_, _) => await OnInstallAsync();
        _btnUninstall.Click += async (_, _) => await OnUninstallAsync();
        _btnScanExtras.Click += async (_, _) => await OnScanExtrasAsync();
        _btnNews.Click += (_, _) => { using var f = new NewsForm(_settings); f.ShowDialog(this); };
        _btnAdmin.Click += (_, _) => RestartAsAdmin();
        _btnAdminBanner.Click += (_, _) => RestartAsAdmin();
        _btnStop.Click += (_, _) => { _logger.Write("Stop requested - finishing up ..."); _cts?.Cancel(); };
        _btnExit.Click += (_, _) => Close();

        // ---- header ----
        var title = new Label
        {
            Text = "AI / Data Science / Full-Stack Environment",
            Font = new Font("Segoe UI", 12f, FontStyle.Bold),
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            Tag = ThemeRole.Default,
            Margin = new Padding(0),
        };

        var filterLabel = new Label
        { Text = "Filter:", AutoSize = true, Anchor = AnchorStyles.Left, Margin = new Padding(0, 6, 4, 0), Tag = ThemeRole.Muted };

        _filter.Width = 260;
        _filter.Anchor = AnchorStyles.Left;
        _filter.TextChanged += (_, _) =>
        {
            _setupTree.ApplyFilter(_filter.Text);
            _btnClearFilter.Visible = _filter.Text.Length > 0;
        };

        _btnClearFilter = new Button
        {
            Text = "×",
            Width = 26,
            Height = 24,
            FlatStyle = FlatStyle.Flat,
            Anchor = AnchorStyles.Left,
            UseMnemonic = false,
            Visible = false,
            Margin = new Padding(0, 0, 0, 0),
            Tag = ThemeRole.Secondary,
        };
        _btnClearFilter.Click += (_, _) => _filter.Text = "";
        _tips.SetToolTip(_btnClearFilter, "Clear filter (Esc)");

        var themeLabel = new Label
        { Text = "Theme:", AutoSize = true, Anchor = AnchorStyles.Left, Margin = new Padding(14, 6, 4, 0), Tag = ThemeRole.Muted };

        _themeBox.DropDownStyle = ComboBoxStyle.DropDownList;
        _themeBox.Width = 90;
        _themeBox.Anchor = AnchorStyles.Left;
        foreach (var t in Themes.All) _themeBox.Items.Add(t.Name);
        _themeBox.SelectedItem = ThemeManager.Current.Name;
        _themeBox.SelectedIndexChanged += (_, _) =>
        {
            if (_themeBox.SelectedItem is string name) ThemeManager.Set(Themes.ByName(name), _settings);
        };

        var filterRow = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = false, Padding = new Padding(0) };
        filterRow.Controls.Add(filterLabel);
        filterRow.Controls.Add(_filter);
        filterRow.Controls.Add(_btnClearFilter);
        filterRow.Controls.Add(themeLabel);
        filterRow.Controls.Add(_themeBox);

        var header = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2, Margin = new Padding(0) };
        header.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
        header.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
        header.Controls.Add(title, 0, 0);
        header.Controls.Add(filterRow, 0, 1);

        // ---- admin banner ----
        _adminBanner.Tag = ThemeRole.WarnBanner;
        _adminBanner.Padding = new Padding(10, 4, 10, 4);

        var bannerText = new Label
        {
            Text = "Some system tweaks need administrator rights.",
            AutoSize = false,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            Tag = ThemeRole.WarnText,
        };
        _btnAdminBanner.Anchor = AnchorStyles.Right;
        _btnAdminBanner.MinimumSize = new Size(150, 28);

        var bannerLayout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1, Margin = new Padding(0) };
        bannerLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        bannerLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        bannerLayout.Controls.Add(bannerText, 0, 0);
        bannerLayout.Controls.Add(_btnAdminBanner, 1, 0);
        _adminBanner.Controls.Add(bannerLayout);

        // ---- tree ----
        _tree.Dock = DockStyle.Fill;
        _tree.Font = new Font("Segoe UI", 9.5f);
        _tree.ItemHeight = 24;
        _tree.Indent = 18;
        _setupTree.SelectionChanged += UpdateCount;
        _setupTree.Build();
        AttachTreeContextMenu();

        // ---- grouped toolbar ----
        var buttons = new FlowLayoutPanel
        { Dock = DockStyle.Fill, AutoSize = true, WrapContents = true, Padding = new Padding(0, 2, 0, 4), Margin = new Padding(0) };
        foreach (var c in new Control[]
        {
            _btnSelectAll, _btnSelectNone, _sep1,
            _btnCheckStatus, _sep2,
            _btnInstall, _btnUninstall, _sep3,
            _btnScanExtras, _btnNews, _sep4,
            _btnAdmin, _btnStop, _btnExit,
        })
            buttons.Controls.Add(c);

        // ---- step label + progress + log ----
        _stepLabel.AutoSize = false;
        _stepLabel.Dock = DockStyle.Fill;
        _stepLabel.Height = 16;
        _stepLabel.Text = "Ready.";
        _stepLabel.TextAlign = ContentAlignment.MiddleLeft;
        _stepLabel.Tag = ThemeRole.Muted;
        _stepLabel.Margin = new Padding(0);

        _progress.Dock = DockStyle.Fill;
        _progress.Height = 14;
        _progress.Margin = new Padding(0, 0, 0, 4);

        _log.Multiline = true;
        _log.ReadOnly = true;
        _log.ScrollBars = RichTextBoxScrollBars.Both;
        _log.WordWrap = false;
        _log.DetectUrls = false;
        _log.Font = new Font("Cascadia Mono", 8.5f);
        _log.Dock = DockStyle.Fill;
        _log.Margin = new Padding(0);

        var bottom = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 4, Margin = new Padding(0) };
        bottom.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        bottom.RowStyles.Add(new RowStyle(SizeType.Absolute, 16));
        bottom.RowStyles.Add(new RowStyle(SizeType.Absolute, 18));
        bottom.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        bottom.Controls.Add(buttons, 0, 0);
        bottom.Controls.Add(_stepLabel, 0, 1);
        bottom.Controls.Add(_progress, 0, 2);
        bottom.Controls.Add(_log, 0, 3);

        _split.Dock = DockStyle.Fill;
        _split.Orientation = Orientation.Horizontal;
        _split.Panel1.Controls.Add(_tree);
        _split.Panel2.Controls.Add(bottom);
        _split.Margin = new Padding(0);

        // ---- page layout ----
        var page = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            Padding = new Padding(10, 8, 10, 6),
            Margin = new Padding(0),
        };
        page.RowStyles.Add(new RowStyle(SizeType.Absolute, 64));
        page.RowStyles.Add(new RowStyle(SizeType.Absolute, IsAdmin() ? 0 : 38));
        page.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        page.Controls.Add(header, 0, 0);
        page.Controls.Add(_adminBanner, 0, 1);
        page.Controls.Add(_split, 0, 2);
        Controls.Add(page);

        // ---- status strip ----
        _busyLabel.Text = "Ready";
        _statusLabel.Text = "Status not checked yet.";
        _countLabel.Text = "0 selected";
        _statusStrip.Items.AddRange(new ToolStripItem[] { _busyLabel, _statusLabel, _countLabel });
        Controls.Add(_statusStrip);

        // ---- lifecycle ----
        Load += (_, _) =>
        {
            var defaultSplit = Math.Max(150, (int)(_split.Height * 0.58));
            _split.SplitterDistance = _settings.TreeSplit > 0 ? _settings.TreeSplit : defaultSplit;
            _split.Panel1MinSize = 150;
            _split.Panel2MinSize = 220;
        };

        Shown += async (_, _) =>
        {
            Activate();
            await RefreshStatusAsync();
        };

        FormClosing += OnFormClosing;
        KeyDown += MainForm_KeyDown;

        ThemeManager.Register(this, _setupTree.Restyle);
        _setupTree.Restyle();
        UpdateCount();
    }

    private static Button MakeButton(string text, ThemeRole role = ThemeRole.Default) => new()
    {
        Text = text,
        AutoSize = true,
        AutoSizeMode = AutoSizeMode.GrowAndShrink,
        MinimumSize = new Size(110, 34),
        Padding = new Padding(8, 0, 8, 0),
        UseMnemonic = false,
        Margin = new Padding(2, 2, 2, 2),
        Tag = role,
    };

    private static Label MakeSeparator() => new()
    {
        Width = 1,
        Height = 28,
        Margin = new Padding(6, 4, 6, 4),
        Tag = ThemeRole.Separator,
        Text = "",
    };

    private void AttachTreeContextMenu()
    {
        var menu = new ContextMenuStrip();
        var selectHere = menu.Items.Add("Select this group");
        selectHere.Click += (_, _) =>
        {
            if (_tree.SelectedNode is { } node) _setupTree.SetCheckedForNode(node, true);
        };
        var clearHere = menu.Items.Add("Clear this group");
        clearHere.Click += (_, _) =>
        {
            if (_tree.SelectedNode is { } node) _setupTree.SetCheckedForNode(node, false);
        };
        menu.Items.Add(new ToolStripSeparator());
        var install = menu.Items.Add("Install checked");
        install.Click += async (_, _) => await OnInstallAsync();
        var uninstall = menu.Items.Add("Uninstall checked");
        uninstall.Click += async (_, _) => await OnUninstallAsync();

        menu.Opening += (_, e) =>
        {
            if (_tree.SelectedNode == null || _busy) e.Cancel = true;
        };
        _tree.ContextMenuStrip = menu;
    }

    private void MainForm_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Control && e.KeyCode == Keys.A) { _setupTree.SetAllChecked(true); e.Handled = true; }
        else if (e.Control && e.KeyCode == Keys.D) { _setupTree.SetAllChecked(false); e.Handled = true; }
        else if (e.Control && e.KeyCode == Keys.F) { _filter.Focus(); _filter.SelectAll(); e.Handled = true; }
        else if (e.KeyCode == Keys.F5) { _ = RefreshStatusAsync(); e.Handled = true; }
        else if (e.KeyCode == Keys.Escape && _filter.Focused) { _filter.Text = ""; e.Handled = true; }
        else if (e.KeyCode == Keys.Enter && !_busy && _setupTree.CheckedCount > 0)
        {
            _ = OnInstallAsync();
            e.Handled = true;
        }
    }

    /// <summary>Colour-coded log: errors red, successes green, skips muted, headers accented.</summary>
    private void AppendLog(string line)
    {
        if (IsDisposed) return;
        var t = ThemeManager.Current;

        var color = line switch
        {
            _ when line.Contains("[FAIL]", StringComparison.OrdinalIgnoreCase) => t.Danger,
            _ when line.Contains("ERROR", StringComparison.OrdinalIgnoreCase) => t.Danger,
            _ when line.Contains("[OK]", StringComparison.OrdinalIgnoreCase) => t.Good,
            _ when line.Contains("success", StringComparison.OrdinalIgnoreCase) => t.Good,
            _ when line.Contains("[SKIP]", StringComparison.OrdinalIgnoreCase) => t.Muted,
            _ when line.Contains("WARN", StringComparison.OrdinalIgnoreCase) => t.Warn,
            _ when line.StartsWith("==") || line.StartsWith("──") => t.Accent,
            _ => t.Text,
        };

        _log.SelectionStart = _log.TextLength;
        _log.SelectionLength = 0;
        _log.SelectionColor = color;
        _log.AppendText(line + Environment.NewLine);
        _log.SelectionColor = t.Text;
        _log.SelectionStart = _log.TextLength;
        _log.ScrollToCaret();
    }

    private void UpdateCount()
    {
        var n = _setupTree.CheckedCount;
        _countLabel.Text = $"{n} selected";
        _btnInstall.Text = n > 0 ? $"Install ({n})" : "Install Selected";
        _btnUninstall.Text = n > 0 ? $"Uninstall ({n})" : "Uninstall Selected";
    }
}