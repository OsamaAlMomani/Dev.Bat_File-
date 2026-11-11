using System.Collections;
using System.Diagnostics;
using System.Globalization;

namespace AiSetupUi;

/// <summary>
/// Library News &amp; Updates window. Search the web for fresh AI open-source libraries and headlines,
/// see which catalog packages are outdated, and check this PC. All data comes from NewsService /
/// SystemInfoService; this class only lays things out.
/// </summary>
internal sealed class NewsForm : Form
{
    private enum Page { Libraries, Headlines, Updates, System }

    private static readonly int[] RangeDays = { 7, 30, 90 };

    private readonly AppSettings _settings;
    private readonly CancellationTokenSource _cts = new();
    private readonly List<string> _warnings = new();
    private bool _running;

    private readonly TextBox _query = new();
    private readonly CheckBox _pythonOnly = new();
    private readonly ComboBox _range = new();
    private readonly Button _btnSearch;
    private readonly Button _btnRefreshAll;
    private readonly Button _btnOpen;
    private readonly Button _btnCopyPip;
    private readonly Label _status = new();

    private readonly ListView _libs;
    private readonly ListView _news;
    private readonly ListView _updates;
    private readonly TextBox _system = new();

    private readonly Dictionary<Page, Button> _tabs = new();
    private readonly Dictionary<Page, Control> _views = new();
    private Page _page = Page.Libraries;

    public NewsForm(AppSettings settings)
    {
        _settings = settings;

        Text = "Library News & Updates";
        Size = new Size(1040, 720);
        MinimumSize = new Size(780, 440);
        StartPosition = FormStartPosition.CenterParent;
        Font = new Font("Segoe UI", 9f);

        // ---- search bar ----
        _query.Width = 280;
        _query.Text = settings.NewsQuery;
        _query.KeyDown += async (_, e) =>
        {
            if (e.KeyCode != Keys.Enter) return;
            e.SuppressKeyPress = true;
            await SearchAsync(includeLocal: false);
        };

        _btnSearch = MakeButton("Search", ThemeRole.Primary);
        _btnSearch.Click += async (_, _) => await SearchAsync(includeLocal: false);

        _range.DropDownStyle = ComboBoxStyle.DropDownList;
        _range.Width = 110;
        _range.Items.AddRange(new object[] { "Last 7 days", "Last 30 days", "Last 90 days" });
        var savedIndex = Array.IndexOf(RangeDays, settings.NewsDays);
        _range.SelectedIndex = savedIndex >= 0 ? savedIndex : 1;

        _pythonOnly.Text = "Python only";
        _pythonOnly.AutoSize = true;
        _pythonOnly.Checked = settings.NewsPythonOnly;
        _pythonOnly.Margin = new Padding(10, 6, 0, 0);

        _btnRefreshAll = MakeButton("Refresh All");
        _btnRefreshAll.Click += async (_, _) => await SearchAsync(includeLocal: true);

        var bar = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = false };
        bar.Controls.Add(new Label { Text = "Search:", AutoSize = true, Margin = new Padding(0, 8, 4, 0) });
        bar.Controls.Add(_query);
        bar.Controls.Add(_btnSearch);
        bar.Controls.Add(new Label { Text = "Range:", AutoSize = true, Margin = new Padding(12, 8, 4, 0) });
        bar.Controls.Add(_range);
        bar.Controls.Add(_pythonOnly);
        bar.Controls.Add(_btnRefreshAll);

        // ---- views ----
        _libs = MakeList(("Repository", 230), ("Stars", 65), ("Created", 85), ("Lang", 80),
                         ("pip package", 150), ("In catalog", 75), ("Description", 400));
        _news = MakeList(("Title", 620), ("Points", 60), ("Comments", 75), ("Posted", 130));
        _updates = MakeList(("Package", 280), ("Installed", 130), ("Latest", 130));

        _system.Multiline = true;
        _system.ReadOnly = true;
        _system.ScrollBars = ScrollBars.Vertical;
        _system.Font = new Font("Consolas", 9f);
        _system.Dock = DockStyle.Fill;
        _system.Visible = false;

        _views[Page.Libraries] = _libs;
        _views[Page.Headlines] = _news;
        _views[Page.Updates] = _updates;
        _views[Page.System] = _system;

        var content = new Panel { Dock = DockStyle.Fill };
        foreach (var v in _views.Values) content.Controls.Add(v);

        // ---- tab buttons ----
        var tabs = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = false };
        AddTab(tabs, Page.Libraries, "New Libraries");
        AddTab(tabs, Page.Headlines, "Headlines");
        AddTab(tabs, Page.Updates, "Updates");
        AddTab(tabs, Page.System, "System");

        // ---- action bar ----
        _btnOpen = MakeButton("Open Selected");
        _btnOpen.Click += (_, _) => OpenSelected();
        _btnCopyPip = MakeButton("Copy pip command");
        _btnCopyPip.Click += (_, _) => CopyPipCommand();

        var hint = new Label
        {
            Text = "Double-click a row to open it. Click a column header to sort.",
            AutoSize = true,
            Margin = new Padding(12, 10, 0, 0),
            Tag = ThemeRole.Muted,
        };
        var actions = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = false };
        actions.Controls.Add(_btnOpen);
        actions.Controls.Add(_btnCopyPip);
        actions.Controls.Add(hint);

        _status.Dock = DockStyle.Fill;
        _status.Text = "Starting ...";
        _status.Tag = ThemeRole.Muted;

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 5, Padding = new Padding(10, 6, 10, 6),
        };
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 24));
        layout.Controls.Add(bar, 0, 0);
        layout.Controls.Add(tabs, 0, 1);
        layout.Controls.Add(content, 0, 2);
        layout.Controls.Add(actions, 0, 3);
        layout.Controls.Add(_status, 0, 4);
        Controls.Add(layout);

        ShowPage(Page.Libraries);
        ThemeManager.Register(this, Colorize);

        Shown += async (_, _) => await SearchAsync(includeLocal: true);
        FormClosing += (_, _) => _cts.Cancel();
    }

    // ============================================================
    // Layout helpers
    // ============================================================

    private static Button MakeButton(string text, ThemeRole role = ThemeRole.Default) => new()
    {
        Text = text,
        AutoSize = true,
        AutoSizeMode = AutoSizeMode.GrowAndShrink,
        MinimumSize = new Size(90, 30),
        Padding = new Padding(6, 0, 6, 0),
        UseMnemonic = false,
        Tag = role,
    };

    private void AddTab(FlowLayoutPanel host, Page page, string title)
    {
        var b = MakeButton(title);
        b.MinimumSize = new Size(120, 32);
        b.Click += (_, _) => ShowPage(page);
        _tabs[page] = b;
        host.Controls.Add(b);
    }

    private void SetTabTitle(Page page, string title, int? count) =>
        _tabs[page].Text = count.HasValue ? $"{title} ({count})" : title;

    private void ShowPage(Page page)
    {
        _page = page;
        foreach (var (p, view) in _views) view.Visible = p == page;
        foreach (var (p, button) in _tabs)
        {
            button.Tag = p == page ? ThemeRole.Selected : ThemeRole.Default;
            ThemeManager.Apply(button);
        }
        var list = CurrentList();
        _btnOpen.Enabled = list != null;
        _btnCopyPip.Enabled = page is Page.Libraries or Page.Updates;
    }

    private ListView? CurrentList() => _page switch
    {
        Page.Libraries => _libs,
        Page.Headlines => _news,
        Page.Updates => _updates,
        _ => null,
    };

    private ListView MakeList(params (string Title, int Width)[] columns)
    {
        var lv = new ListView
        {
            Dock = DockStyle.Fill, View = View.Details, FullRowSelect = true, HideSelection = false,
            MultiSelect = false, BorderStyle = BorderStyle.FixedSingle, ShowItemToolTips = true, Visible = false,
        };
        foreach (var (title, width) in columns) lv.Columns.Add(title, width);

        ThemeManager.PrepareListView(lv);
        lv.DoubleClick += (_, _) => OpenSelected();

        // The sorter is attached on the first header click, so the initial order (most stars first) is kept.
        var sorter = new RowSorter();
        lv.ColumnClick += (_, e) =>
        {
            sorter.Descending = sorter.Column == e.Column && !sorter.Descending;
            sorter.Column = e.Column;
            lv.ListViewItemSorter ??= sorter;
            lv.Sort();
        };
        return lv;
    }

    /// <summary>Sorts rows by one column: numbers as numbers, everything else (ISO dates included) as text.</summary>
    private sealed class RowSorter : IComparer
    {
        public int Column { get; set; } = -1;
        public bool Descending { get; set; }

        public int Compare(object? x, object? y)
        {
            if (Column < 0) return 0;
            var a = CellText(x);
            var b = CellText(y);

            var result =
                double.TryParse(a.Replace(",", ""), NumberStyles.Float, CultureInfo.InvariantCulture, out var da) &&
                double.TryParse(b.Replace(",", ""), NumberStyles.Float, CultureInfo.InvariantCulture, out var db)
                    ? da.CompareTo(db)
                    : string.Compare(a, b, StringComparison.OrdinalIgnoreCase);
            return Descending ? -result : result;
        }

        private string CellText(object? o) =>
            o is ListViewItem item && Column < item.SubItems.Count ? item.SubItems[Column].Text : "";
    }

    // ============================================================
    // Theme
    // ============================================================

    /// <summary>Row colours depend on the theme, so they're set here instead of when rows are created.</summary>
    private void Colorize()
    {
        var t = ThemeManager.Current;

        foreach (ListViewItem it in _libs.Items)
            it.ForeColor = it.Tag switch
            {
                RepoInfo { InCatalog: true } => t.Muted,   // you already have it
                RepoInfo { PipName: not null } => t.Good,  // new and installable right now
                _ => t.Text,
            };

        foreach (ListViewItem it in _news.Items) it.ForeColor = t.Text;
        foreach (ListViewItem it in _updates.Items) it.ForeColor = it.Tag is null ? t.Muted : t.Text;
    }

    // ============================================================
    // Searching
    // ============================================================

    private async Task SearchAsync(bool includeLocal)
    {
        if (_running) return;
        _running = true;
        SetBusy(true);
        _warnings.Clear();

        var query = _query.Text.Trim();
        var days = RangeDays[Math.Max(0, _range.SelectedIndex)];
        var pythonOnly = _pythonOnly.Checked;

        _settings.NewsQuery = query;
        _settings.NewsDays = days;
        _settings.NewsPythonOnly = pythonOnly;
        _settings.Save();

        _status.Text = includeLocal
            ? "Fetching libraries, headlines, outdated packages and system info (pip can take a while) ..."
            : query.Length > 0 ? $"Searching for '{query}' ..." : "Fetching the latest libraries and headlines ...";

        try
        {
            var ct = _cts.Token;
            var tasks = new List<Task>
            {
                Guard("Libraries", () => LoadLibrariesAsync(query, pythonOnly, days, ct)),
                Guard("Headlines", () => LoadHeadlinesAsync(query, days, ct)),
            };
            if (includeLocal)
            {
                tasks.Add(Guard("System info", () => LoadSystemAsync(ct)));
                tasks.Add(Guard("Updates", () => LoadUpdatesAsync(ct)));
            }
            await Task.WhenAll(tasks);

            var done = $"Done. Last updated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}";
            _status.Text = _warnings.Count == 0 ? done : $"{done}   Note: {string.Join("  |  ", _warnings.Distinct())}";
        }
        catch (OperationCanceledException)
        {
            // window was closed mid-fetch
        }
        catch (Exception ex)
        {
            if (!IsDisposed) _status.Text = "Something went wrong: " + ex.Message;
        }
        finally
        {
            _running = false;
            if (!IsDisposed) SetBusy(false);
        }
    }

    private void SetBusy(bool busy)
    {
        _btnSearch.Enabled = !busy;
        _btnRefreshAll.Enabled = !busy;
        UseWaitCursor = busy;
    }

    /// <summary>One section failing shouldn't blank the others.</summary>
    private async Task Guard(string section, Func<Task> work)
    {
        try { await work(); }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex) { _warnings.Add($"{section}: {ex.Message}"); }
    }

    private async Task LoadLibrariesAsync(string query, bool pythonOnly, int days, CancellationToken ct)
    {
        var result = await NewsService.GetNewLibrariesAsync(query, pythonOnly, days, ct);
        if (IsDisposed) return;

        Fill(_libs, result.Items.Select(LibraryRow));
        SetTabTitle(Page.Libraries, "New Libraries", result.Items.Count);
        _warnings.AddRange(result.Warnings);
    }

    private async Task LoadHeadlinesAsync(string query, int days, CancellationToken ct)
    {
        var result = await NewsService.GetHeadlinesAsync(query, days, ct);
        if (IsDisposed) return;

        Fill(_news, result.Items.Select(HeadlineRow));
        SetTabTitle(Page.Headlines, "Headlines", result.Items.Count);
        _warnings.AddRange(result.Warnings);
    }

    private async Task LoadUpdatesAsync(CancellationToken ct)
    {
        var outdated = await NewsService.GetOutdatedCatalogAsync(ct);
        if (IsDisposed) return;

        if (outdated == null)
        {
            Fill(_updates, new[] { new ListViewItem("(could not query PyPI - is pip available?)") });
            SetTabTitle(Page.Updates, "Updates", null);
            _warnings.Add("Could not check for outdated packages.");
            return;
        }

        var rows = outdated.Count == 0
            ? new[] { new ListViewItem("All catalog packages are up to date.") }
            : outdated.Select(UpdateRow).ToArray();
        Fill(_updates, rows);
        SetTabTitle(Page.Updates, "Updates", outdated.Count);
    }

    private async Task LoadSystemAsync(CancellationToken ct)
    {
        var text = await SystemInfoService.BuildAsync(ct);
        if (!IsDisposed) _system.Text = text;
    }

    // ============================================================
    // Rows
    // ============================================================

    private static ListViewItem LibraryRow(RepoInfo r)
    {
        var it = new ListViewItem(r.FullName) { Tag = r, ToolTipText = r.Description };
        it.SubItems.Add(r.Stars.ToString("N0", CultureInfo.InvariantCulture));
        it.SubItems.Add(r.Created?.ToString("yyyy-MM-dd") ?? "");
        it.SubItems.Add(r.Language);
        it.SubItems.Add(r.PipName != null ? $"{r.PipName} {r.PipVersion}".Trim() : "");
        it.SubItems.Add(r.InCatalog ? "yes" : "");
        it.SubItems.Add(r.Description);
        return it;
    }

    private static ListViewItem HeadlineRow(Headline h)
    {
        var it = new ListViewItem(h.Title) { Tag = h, ToolTipText = h.Url };
        it.SubItems.Add(h.Points.ToString(CultureInfo.InvariantCulture));
        it.SubItems.Add(h.Comments.ToString(CultureInfo.InvariantCulture));
        it.SubItems.Add(h.Posted?.LocalDateTime.ToString("yyyy-MM-dd HH:mm") ?? "");
        return it;
    }

    private static ListViewItem UpdateRow(PipOutdated p)
    {
        var it = new ListViewItem(p.Name) { Tag = p };
        it.SubItems.Add(p.Version);
        it.SubItems.Add(p.Latest);
        return it;
    }

    private void Fill(ListView lv, IEnumerable<ListViewItem> rows)
    {
        lv.BeginUpdate();
        try
        {
            lv.Items.Clear();
            lv.Items.AddRange(rows.ToArray());
            if (lv.Columns.Count > 0) lv.Columns[^1].Width = -2; // last column takes the remaining width
        }
        finally
        {
            lv.EndUpdate();
        }
        Colorize();
    }

    // ============================================================
    // Row actions
    // ============================================================

    private static string? UrlFor(ListViewItem item) => item.Tag switch
    {
        RepoInfo r => r.Url,
        Headline h => h.Url,
        PipOutdated p => $"https://pypi.org/project/{Uri.EscapeDataString(p.Name)}/",
        _ => null,
    };

    private void OpenSelected()
    {
        var list = CurrentList();
        if (list == null || list.SelectedItems.Count == 0) return;

        var url = UrlFor(list.SelectedItems[0]);
        // Only ever hand real web links to the shell.
        if (url == null || !Uri.TryCreate(url, UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttps && uri.Scheme != Uri.UriSchemeHttp))
            return;

        try { Process.Start(new ProcessStartInfo(uri.AbsoluteUri) { UseShellExecute = true }); }
        catch (Exception ex) { _status.Text = "Could not open the link: " + ex.Message; }
    }

    private void CopyPipCommand()
    {
        var list = CurrentList();
        if (list == null || list.SelectedItems.Count == 0)
        {
            _status.Text = "Select a row first.";
            return;
        }

        var command = list.SelectedItems[0].Tag switch
        {
            RepoInfo { PipName: not null } r => $"pip install {r.PipName}",
            PipOutdated p => $"pip install -U {p.Name}",
            _ => null,
        };

        if (command == null)
        {
            _status.Text = "No matching PyPI package was found for that repository - open it to see how it is installed.";
            return;
        }

        try
        {
            Clipboard.SetText(command);
            _status.Text = $"Copied: {command}";
        }
        catch (Exception ex)
        {
            _status.Text = "Could not use the clipboard: " + ex.Message;
        }
    }
}
