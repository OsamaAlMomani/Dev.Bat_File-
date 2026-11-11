using System.ComponentModel;
using System.Diagnostics;
using System.Security.Principal;

namespace AiSetupUi;

// Everything the buttons do: busy handling, install, uninstall, scan, status refresh.
internal sealed partial class MainForm
{
    private bool _busy;
    private bool _closeWhenIdle;
    private CancellationTokenSource? _cts;

    // ============================================================
    // Admin
    // ============================================================

    private static bool IsAdmin()
    {
        using var id = WindowsIdentity.GetCurrent();
        return new WindowsPrincipal(id).IsInRole(WindowsBuiltInRole.Administrator);
    }

    private void RestartAsAdmin()
    {
        if (MessageBox.Show(this, "Close this window and reopen it with administrator rights?\n\nThe registry tweaks need that.",
                "Restart as Administrator", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
            return;

        try
        {
            Process.Start(new ProcessStartInfo(Environment.ProcessPath!) { UseShellExecute = true, Verb = "runas" });
            Close();
        }
        catch (Win32Exception)
        {
            // user said no at the UAC prompt - stay put
        }
    }

    // ============================================================
    // Busy handling
    // ============================================================

    private void SetBusy(bool busy)
    {
        foreach (var b in _lockWhileBusy) b.Enabled = !busy;

        _btnStop.Visible = busy;
        _btnStop.Enabled = busy;

        _progress.Style = busy ? ProgressBarStyle.Marquee : ProgressBarStyle.Continuous;
        if (!busy)
        {
            _progress.Value = 0;
            _stepLabel.Text = "Ready.";
        }

        _busyLabel.Text = busy ? "Working…" : "Ready";

        if (!busy) UpdateCount();
    }

    private void OnProgress((int Done, int Total) p)
    {
        if (!_busy) return;
        _progress.Style = ProgressBarStyle.Continuous;
        _progress.Maximum = Math.Max(1, p.Total);
        _progress.Value = Math.Min(p.Done, _progress.Maximum);
        var pct = 100 * p.Done / Math.Max(1, p.Total);
        _stepLabel.Text = $"Step {p.Done} of {p.Total}  ({pct}%)";
    }

    /// <summary>Runs one long operation at a time, keeps the window responsive, and unlocks the buttons afterwards.</summary>
    private async Task RunBusyAsync(Func<CancellationToken, Task> work)
    {
        if (_busy) return;
        _busy = true;
        _cts = new CancellationTokenSource();
        SetBusy(true);

        try
        {
            await work(_cts.Token);
        }
        catch (OperationCanceledException)
        {
            _logger.Write("Stopped. Anything already finished stays as it is.");
        }
        catch (Exception ex)
        {
            _logger.Write("Unexpected error: " + ex.Message);
            MessageBox.Show(this, ex.Message, "Something went wrong", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            _busy = false;
            _cts.Dispose();
            _cts = null;
            SetBusy(false);
            if (_closeWhenIdle) Close();
        }
    }

    private void OnFormClosing(object? sender, FormClosingEventArgs e)
    {
        // Persist geometry before we decide whether to cancel the close.
        _settings.WindowWidth = Width;
        _settings.WindowHeight = Height;
        _settings.TreeSplit = _split.SplitterDistance;
        _settings.Save();

        if (!_busy) return;

        var answer = MessageBox.Show(this, "Something is still running. Stop it and exit?", "Still working",
            MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
        e.Cancel = true;
        if (answer == DialogResult.Yes)
        {
            _closeWhenIdle = true;
            _cts?.Cancel();
        }
    }

    // ============================================================
    // Install
    // ============================================================

    private async Task OnInstallAsync()
    {
        var selected = _setupTree.CheckedItems();
        if (selected.Count == 0)
        {
            MessageBox.Show(this, "Nothing is checked, so there's nothing to install. Check the items you want first.",
                "Nothing selected", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        // Extras are already on the PC, so they belong to uninstall, not install.
        var extras = selected.Where(i => i.IsExtra).ToList();
        if (extras.Count > 0)
        {
            MessageBox.Show(this,
                $"{extras.Count} of the checked item(s) are 'extra' packages already on your PC (from Scan System Extras). " +
                "Those will be skipped for install - use 'Uninstall Selected' to remove them.",
                "Extra packages skipped for install", MessageBoxButtons.OK, MessageBoxIcon.Information);
            selected = selected.Where(i => !i.IsExtra).ToList();
            if (selected.Count == 0) return;
        }

        var tweaks = selected.Where(i => i.Type == ItemType.Tweak).ToList();
        var msg = $"This will install {selected.Count} selected item(s).";
        if (tweaks.Count > 0)
        {
            msg += $"\n\nThis includes {tweaks.Count} system tweak(s) that change Windows settings:\n - " +
                   string.Join("\n - ", tweaks.Select(t => t.Name));
            if (!IsAdmin() && tweaks.Any(t => t.Tweak!.Action is TweakAction.DevMode or TweakAction.LongPath or TweakAction.Wsl))
                msg += "\n\nHeads up: the registry and WSL tweaks need administrator rights. Use 'Restart as Admin' first if you want them to work.";
        }
        msg += "\n\nNothing else on your machine will be modified or removed. Continue?";

        if (MessageBox.Show(this, msg, "Confirm installation", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
            return;

        Tally? tally = null;
        await RunBusyAsync(async ct =>
        {
            _logger.StartSection("AI / Data Science / Full-Stack Environment - Setup Log", selected.Count);
            var service = new SetupService(_logger);
            tally = await service.InstallAsync(selected, new Progress<(int, int)>(OnProgress), ct);
        });

        if (tally != null)
        {
            var summary = $"Finished.\n\nOK: {tally.Ok}\nFailed: {tally.Fail}";
            if (tally.Failed.Count > 0) summary += "\n\nFailed items:\n - " + string.Join("\n - ", tally.Failed);
            MessageBox.Show(this, summary, "Setup finished", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        await RefreshStatusAsync();
    }

    // ============================================================
    // Uninstall
    // ============================================================

    private async Task OnUninstallAsync()
    {
        var selected = _setupTree.CheckedItems();
        if (selected.Count == 0)
        {
            MessageBox.Show(this, "Nothing is checked, so there's nothing to uninstall. Check the items you want to remove first.",
                "Nothing selected", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        var tweaks = selected.Where(i => i.Type == ItemType.Tweak).ToList();
        var dirsSelected = selected.Any(i => i.Type == ItemType.Project && i.Id == "DIRS");

        var msg = $"This will UNINSTALL / REVERT {selected.Count} selected item(s).";
        if (tweaks.Count > 0)
        {
            msg += $"\n\nThis includes {tweaks.Count} system tweak(s) that will be REVERSED:\n - " +
                   string.Join("\n - ", tweaks.Select(t => t.Name));
            if (tweaks.Any(t => t.Id == "WSL2"))
                msg += "\n\nWARNING: WSL2 can't be reversed automatically. The log will show the command to disable it yourself. " +
                       "Installed distros stay on disk either way.";
        }
        if (dirsSelected)
            msg += "\n\nNOTE: your projects\\ folder is NOT auto-deleted (it may contain your work). " +
                   "You'll get a log line reminding you to remove it manually if you want.";
        msg += "\n\nContinue?";

        if (MessageBox.Show(this, msg, "Confirm uninstall", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
            return;

        Tally? tally = null;
        await RunBusyAsync(async ct =>
        {
            _logger.StartSection("UNINSTALL / REVERT LOG", selected.Count);
            var service = new SetupService(_logger);
            tally = await service.UninstallAsync(selected, new Progress<(int, int)>(OnProgress), ct);
        });

        if (tally != null)
        {
            var summary = $"Uninstall finished.\n\nOK: {tally.Ok}\nFailed: {tally.Fail}\nSkipped: {tally.Skip}";
            if (tally.Failed.Count > 0) summary += "\n\nFailed items:\n - " + string.Join("\n - ", tally.Failed);
            MessageBox.Show(this, summary, "Uninstall finished", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        await RefreshStatusAsync();
    }

    // ============================================================
    // Scan system extras
    // ============================================================

    private async Task OnScanExtrasAsync()
    {
        await RunBusyAsync(async ct =>
        {
            _logger.Write("Scanning installed pip packages (read-only) ...");
            _setupTree.RemoveExtras();

            var pkgs = await PipService.ListAsync(ct);
            if (pkgs == null)
            {
                _logger.Write($"  Could not read pip list (is pip available via {AppPaths.PythonExe}?).");
                MessageBox.Show(this, "Could not read pip list. Make sure Python + pip are available (or that .venv exists).",
                    "Scan failed", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // pip, setuptools and wheel are what pip itself runs on - not something to offer for removal.
            var infrastructure = new HashSet<string> { "pip", "setuptools", "wheel" };

            var extras = pkgs
                .Where(p => !Catalog.PipNames.Contains(PipName.Normalize(p.Name)))
                .Where(p => !infrastructure.Contains(PipName.Normalize(p.Name)))
                .OrderBy(p => p.Name, StringComparer.OrdinalIgnoreCase)
                .ToList();

            _setupTree.ShowExtras(extras);

            _logger.Write($"  Found {extras.Count} extra package(s). Check the ones you want, then click 'Uninstall Selected'.");
            _statusLabel.Text = $"Scan: {extras.Count} extra pip package(s) not in catalog.";
        });
        UpdateCount();
    }

    // ============================================================
    // Install-status checking (read-only)
    // ============================================================

    private async Task RefreshStatusAsync()
    {
        await RunBusyAsync(async ct =>
        {
            _statusLabel.Text = "Checking what's already installed ...";
            _logger.Write("Checking install status of every item (read-only - nothing is changed) ...");

            var snapshot = await StatusService.ScanAsync(_logger, ct);
            var (installed, missing, unknown) = _setupTree.ApplyStatus(snapshot);

            var summary = $"Status: {installed} installed, {missing} not installed";
            if (unknown > 0) summary += $", {unknown} unknown";
            _statusLabel.Text = summary;
            _logger.Write($"  Done. {summary}. (Detection only - nothing was installed, changed, or removed.)");
        });
    }
}