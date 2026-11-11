using System.Security;
using System.Text;
using Microsoft.Win32;

namespace AiSetupUi;

/// <summary>Does the actual installing / uninstalling for whatever the user checked.</summary>
internal sealed class SetupService
{
    // winget exit codes that really mean "already there"
    private static readonly int[] WingetAlreadyInstalled =
    {
        unchecked((int)0x8A15002B), // no applicable upgrade
        unchecked((int)0x8A150061), // package already installed
    };

    private const string DevModeSubKey = @"SOFTWARE\Microsoft\Windows\CurrentVersion\AppModelUnlock";
    private const string LongPathSubKey = @"SYSTEM\CurrentControlSet\Control\FileSystem";

    private static readonly UTF8Encoding Utf8NoBom = new(false);

    private readonly Logger _log;

    public SetupService(Logger log) => _log = log;

    // ------------------------------------------------------------
    // INSTALL
    // ------------------------------------------------------------

    public async Task<Tally> InstallAsync(IReadOnlyList<SetupItem> selected,
        IProgress<(int Done, int Total)> progress, CancellationToken ct)
    {
        var tally = new Tally();

        var wingetItems = selected.Where(i => i.Type == ItemType.Winget).ToList();
        var tweakItems = selected.Where(i => i.Type == ItemType.Tweak).ToList();
        var pipItems = DistinctPip(selected.Where(i => i.Type == ItemType.Pip && !i.IsExtra));
        var projectIds = selected.Where(i => i.Type == ItemType.Project).Select(i => i.Id).ToHashSet();

        int total = wingetItems.Count + tweakItems.Count + pipItems.Count + projectIds.Count;
        int done = 0;
        void Advance() => progress.Report((++done, total));
        progress.Report((0, total));

        // ---- winget apps ----
        bool wingetNeeded = wingetItems.Count > 0 || tweakItems.Any(t => t.Tweak!.Action == TweakAction.Winget);
        bool wingetReady = !wingetNeeded || await ProcessRunner.IsAvailableAsync("winget", "--version", ct);
        if (!wingetReady) _log.Write("winget was not found on this PC - winget items and tweaks will be marked as failed.");

        foreach (var item in wingetItems)
        {
            ct.ThrowIfCancellationRequested();
            if (!wingetReady) { tally.Bad(item.Name); Advance(); continue; }

            _log.Write($"Installing (winget): {item.Name} [{item.Id}] ...");
            if (await WingetAsync("install", item.Id, item.Name, ct)) tally.Ok++; else tally.Bad(item.Name);
            Advance();
        }

        // ---- system tweaks ----
        foreach (var item in tweakItems)
        {
            ct.ThrowIfCancellationRequested();
            var tweak = item.Tweak!;
            _log.Write($"Applying system tweak: {item.Name} ...");

            if (tweak.Action == TweakAction.Winget && !wingetReady)
            {
                _log.Write($"  FAILED: {item.Name} - winget not available");
                tally.Bad(item.Name);
                Advance();
                continue;
            }

            try
            {
                await ApplyTweakAsync(tweak, ct);
                _log.Write($"  OK: {item.Name}");
                tally.Ok++;
            }
            catch (OperationCanceledException) { throw; }
            catch (Exception ex)
            {
                _log.Write($"  FAILED: {item.Name} - {ex.Message}");
                tally.Bad(item.Name);
            }
            Advance();
        }

        // ---- virtual environment ----
        if (projectIds.Contains("VENV"))
        {
            ct.ThrowIfCancellationRequested();
            _log.Write($"Creating virtual environment at {AppPaths.VenvDir} ...");
            var r = await ProcessRunner.RunAsync("python", new[] { "-m", "venv", AppPaths.VenvDir }, ct);
            if (r.ExitCode == 0 && r.Started)
            {
                _log.Write("  OK: virtual environment created");
                tally.Ok++;
            }
            else
            {
                _log.Write($"  FAILED: virtual environment - {FailureReason(r, "python -m venv failed")}");
                tally.Bad("Virtual environment");
            }
            Advance();
        }

        // ---- pip packages ----
        if (pipItems.Count > 0)
        {
            var py = AppPaths.PythonExe;
            if (!await ProcessRunner.IsAvailableAsync(py, "--version", ct))
            {
                _log.Write($"Python could not be started ({py}) - pip packages will be marked as failed.");
                _log.Write("  Install Python first (or tick the venv option once Python is on PATH).");
                foreach (var item in pipItems) { tally.Bad(item.Name); Advance(); }
            }
            else
            {
                _log.Write("Upgrading pip/setuptools/wheel ...");
                await ProcessRunner.RunAsync(py, new[] { "-m", "pip", "install", "--upgrade", "pip", "setuptools", "wheel" }, ct);

                foreach (var item in pipItems)
                {
                    ct.ThrowIfCancellationRequested();
                    _log.Write($"Installing (pip): {item.Name} ...");
                    var r = await ProcessRunner.RunAsync(py, new[] { "-m", "pip", "install", item.Id }, ct);
                    if (r.Started && r.ExitCode == 0)
                    {
                        _log.Write($"  OK: {item.Name}");
                        tally.Ok++;
                    }
                    else
                    {
                        _log.Write($"  FAILED: {item.Name} (exit {r.ExitCode}) {r.Tail()}");
                        tally.Bad(item.Name);
                    }
                    Advance();
                }
            }
        }

        var pyExe = AppPaths.PythonExe;

        // ---- project folders ----
        if (projectIds.Contains("DIRS"))
        {
            ct.ThrowIfCancellationRequested();
            _log.Write("Creating project folder structure ...");
            try
            {
                foreach (var d in Catalog.ProjectDirs)
                    Directory.CreateDirectory(Path.Combine(AppPaths.ProjectsDir, d));
                _log.Write("  OK: project folders");
                tally.Ok++;
            }
            catch (Exception ex)
            {
                _log.Write($"  FAILED: project folders - {ex.Message}");
                tally.Bad("Project folders");
            }
            Advance();
        }

        // ---- Jupyter kernel ----
        if (projectIds.Contains("IPYKERNEL"))
        {
            ct.ThrowIfCancellationRequested();
            _log.Write("Registering Jupyter kernel ...");
            await ProcessRunner.RunAsync(pyExe, new[] { "-m", "pip", "install", "ipykernel" }, ct);
            var r = await ProcessRunner.RunAsync(pyExe,
                new[] { "-m", "ipykernel", "install", "--user", "--name", "ai-fullstack", "--display-name", "AI Fullstack" }, ct);
            if (r.Started && r.ExitCode == 0)
            {
                _log.Write("  OK: Jupyter kernel registered");
                tally.Ok++;
            }
            else
            {
                _log.Write($"  FAILED: Jupyter kernel - {FailureReason(r, "ipykernel install failed")}");
                tally.Bad("Jupyter kernel");
            }
            Advance();
        }

        // ---- package inventory ----
        if (projectIds.Contains("FREEZE"))
        {
            ct.ThrowIfCancellationRequested();
            _log.Write("Writing package inventory ...");
            var r = await ProcessRunner.RunAsync(pyExe, new[] { "-m", "pip", "freeze" }, ct);
            try
            {
                if (!r.Started || r.ExitCode != 0) throw new InvalidOperationException(FailureReason(r, "pip freeze failed"));
                File.WriteAllText(AppPaths.InstalledPackagesFile, r.Stdout, Utf8NoBom);
                File.WriteAllText(AppPaths.RequirementsFile, r.Stdout, Utf8NoBom);
                _log.Write("  OK: installed_packages.txt / requirements-full.txt written");
                tally.Ok++;
            }
            catch (Exception ex)
            {
                _log.Write($"  FAILED: package inventory - {ex.Message}");
                tally.Bad("Package inventory");
            }
            Advance();
        }

        _log.Write("============================================================");
        _log.Write($"DONE. OK={tally.Ok} FAILED={tally.Fail}");
        if (tally.Failed.Count > 0) _log.Write($"Failed items: {string.Join(", ", tally.Failed)}");
        _log.Write("Nothing outside your selection was installed, changed, or removed.");
        _log.Write($"Full log: {AppPaths.LogFile}");
        return tally;
    }

    // ------------------------------------------------------------
    // UNINSTALL
    // ------------------------------------------------------------

    public async Task<Tally> UninstallAsync(IReadOnlyList<SetupItem> selected,
        IProgress<(int Done, int Total)> progress, CancellationToken ct)
    {
        var tally = new Tally();

        var wingetItems = selected.Where(i => i.Type == ItemType.Winget).ToList();
        var tweakItems = selected.Where(i => i.Type == ItemType.Tweak).ToList();
        var pipItems = DistinctPip(selected.Where(i => i.Type == ItemType.Pip)); // catalog items and extras
        var projectIds = selected.Where(i => i.Type == ItemType.Project).Select(i => i.Id).ToHashSet();

        int total = wingetItems.Count + tweakItems.Count + pipItems.Count + projectIds.Count;
        int done = 0;
        void Advance() => progress.Report((++done, total));
        progress.Report((0, total));

        // ---- winget apps ----
        bool wingetNeeded = wingetItems.Count > 0 || tweakItems.Any(t => t.Tweak!.Action == TweakAction.Winget);
        bool wingetReady = !wingetNeeded || await ProcessRunner.IsAvailableAsync("winget", "--version", ct);
        if (!wingetReady) _log.Write("winget was not found on this PC - winget items and tweaks will be marked as failed.");

        foreach (var item in wingetItems)
        {
            ct.ThrowIfCancellationRequested();
            if (!wingetReady) { tally.Bad(item.Name); Advance(); continue; }

            _log.Write($"Uninstalling (winget): {item.Name} [{item.Id}] ...");
            if (await WingetAsync("uninstall", item.Id, item.Name, ct)) tally.Ok++; else tally.Bad(item.Name);
            Advance();
        }

        // ---- tweaks: reverse each one ----
        foreach (var item in tweakItems)
        {
            ct.ThrowIfCancellationRequested();
            var tweak = item.Tweak!;
            _log.Write($"Reverting system tweak: {item.Name} ...");

            if (tweak.Action == TweakAction.Winget && !wingetReady)
            {
                _log.Write($"  FAILED: {item.Name} - winget not available");
                tally.Bad(item.Name);
                Advance();
                continue;
            }

            try
            {
                bool reverted = await RevertTweakAsync(tweak, ct);
                if (reverted)
                {
                    _log.Write($"  OK: {item.Name}");
                    tally.Ok++;
                }
                else
                {
                    tally.Skip++;
                }
            }
            catch (OperationCanceledException) { throw; }
            catch (Exception ex)
            {
                _log.Write($"  FAILED: {item.Name} - {ex.Message}");
                tally.Bad(item.Name);
            }
            Advance();
        }

        // ---- pip packages ----
        var py = AppPaths.PythonExe;
        foreach (var item in pipItems)
        {
            ct.ThrowIfCancellationRequested();
            var label = item.IsExtra ? $"{item.Name} (extra)" : item.Name;
            _log.Write($"Uninstalling (pip): {label} ...");
            var r = await ProcessRunner.RunAsync(py, new[] { "-m", "pip", "uninstall", "-y", item.Id }, ct);
            if (r.Started && r.ExitCode == 0)
            {
                _log.Write($"  OK: {label}");
                tally.Ok++;
            }
            else
            {
                _log.Write($"  FAILED: {label} (exit {r.ExitCode}) {r.Tail()}");
                tally.Bad(label);
            }
            Advance();
        }

        // ---- project items ----
        if (projectIds.Contains("VENV"))
        {
            ct.ThrowIfCancellationRequested();
            _log.Write($"Removing virtual environment at {AppPaths.VenvDir} ...");
            try
            {
                if (Directory.Exists(AppPaths.VenvDir))
                {
                    Directory.Delete(AppPaths.VenvDir, recursive: true);
                    _log.Write("  OK: .venv removed");
                    tally.Ok++;
                }
                else
                {
                    _log.Write("  SKIP: .venv not found");
                    tally.Skip++;
                }
            }
            catch (Exception ex)
            {
                _log.Write($"  FAILED: .venv removal - {ex.Message}");
                tally.Bad(".venv");
            }
            Advance();
        }

        if (projectIds.Contains("DIRS"))
        {
            _log.Write("SKIP: projects\\ folder is NOT auto-deleted (it may contain your work).");
            _log.Write($"      Delete it yourself if you want it gone: {AppPaths.ProjectsDir}");
            tally.Skip++;
            Advance();
        }

        if (projectIds.Contains("FREEZE"))
        {
            ct.ThrowIfCancellationRequested();
            _log.Write("Removing generated requirements files ...");
            try
            {
                if (File.Exists(AppPaths.InstalledPackagesFile)) File.Delete(AppPaths.InstalledPackagesFile);
                if (File.Exists(AppPaths.RequirementsFile)) File.Delete(AppPaths.RequirementsFile);
                _log.Write("  OK: installed_packages.txt / requirements-full.txt removed");
                tally.Ok++;
            }
            catch (Exception ex)
            {
                _log.Write($"  FAILED: requirements file removal - {ex.Message}");
                tally.Bad("Requirements files");
            }
            Advance();
        }

        if (projectIds.Contains("IPYKERNEL"))
        {
            ct.ThrowIfCancellationRequested();
            _log.Write("Removing Jupyter kernel 'ai-fullstack' ...");
            var r = await ProcessRunner.RunAsync(py, new[] { "-m", "jupyter", "kernelspec", "remove", "ai-fullstack", "-y" }, ct);
            if (r.Started && r.ExitCode == 0)
            {
                _log.Write("  OK: kernel removed");
                tally.Ok++;
            }
            else
            {
                _log.Write($"  FAILED: kernelspec remove (exit {r.ExitCode}) {r.Tail()}");
                tally.Bad("Jupyter kernel");
            }
            Advance();
        }

        _log.Write("============================================================");
        _log.Write($"DONE. OK={tally.Ok} FAILED={tally.Fail} SKIPPED={tally.Skip}");
        if (tally.Failed.Count > 0) _log.Write($"Failed items: {string.Join(", ", tally.Failed)}");
        _log.Write($"Full log: {AppPaths.LogFile}");
        return tally;
    }

    // ------------------------------------------------------------
    // Helpers
    // ------------------------------------------------------------

    /// <summary>Some packages appear in more than one category (vllm, statsmodels) - only act on them once.</summary>
    private static List<SetupItem> DistinctPip(IEnumerable<SetupItem> items) =>
        items.GroupBy(i => PipName.Normalize(i.Id)).Select(g => g.First()).ToList();

    private async Task<bool> WingetAsync(string verb, string id, string name, CancellationToken ct)
    {
        var args = new List<string> { verb, "--id", id, "-e" };
        if (verb == "install")
        {
            args.Add("--accept-source-agreements");
            args.Add("--accept-package-agreements");
        }

        var r = await ProcessRunner.RunAsync("winget", args, ct);

        if (r.Started && r.ExitCode == 0)
        {
            _log.Write($"  OK: {name}");
            return true;
        }
        if (r.Started && verb == "install" && WingetAlreadyInstalled.Contains(r.ExitCode))
        {
            _log.Write($"  OK: {name} (already installed, nothing newer available)");
            return true;
        }

        _log.Write($"  FAILED: {name} (exit {r.ExitCode}) {FailureReason(r, "")}".TrimEnd());
        return false;
    }

    private async Task ApplyTweakAsync(Tweak tweak, CancellationToken ct)
    {
        switch (tweak.Action)
        {
            case TweakAction.Winget:
            {
                var r = await ProcessRunner.RunAsync("winget",
                    new[] { "install", "--id", tweak.WingetId!, "-e", "--accept-source-agreements", "--accept-package-agreements" }, ct);
                bool ok = r.Started && (r.ExitCode == 0 || WingetAlreadyInstalled.Contains(r.ExitCode));
                if (!ok) throw new InvalidOperationException($"winget exit {r.ExitCode}");
                break;
            }
            case TweakAction.Wsl:
            {
                var status = await ProcessRunner.RunAsync("wsl", new[] { "--status" }, ct);
                if (!status.Started || status.ExitCode != 0)
                {
                    var r = await ProcessRunner.RunAsync("wsl", new[] { "--install", "--no-distribution" }, ct);
                    if (!r.Started || r.ExitCode != 0)
                        throw new InvalidOperationException("wsl install failed - may need a manual reboot + rerun");
                }
                break;
            }
            case TweakAction.DevMode:
                SetMachineDword(DevModeSubKey, "AllowDevelopmentWithoutDevLicense", 1);
                break;
            case TweakAction.LongPath:
                SetMachineDword(LongPathSubKey, "LongPathsEnabled", 1);
                break;
        }
    }

    /// <summary>Returns false when the tweak can't be reversed automatically (WSL).</summary>
    private async Task<bool> RevertTweakAsync(Tweak tweak, CancellationToken ct)
    {
        switch (tweak.Action)
        {
            case TweakAction.Winget:
            {
                var r = await ProcessRunner.RunAsync("winget", new[] { "uninstall", "--id", tweak.WingetId!, "-e" }, ct);
                if (!r.Started || r.ExitCode != 0) throw new InvalidOperationException($"winget uninstall exit {r.ExitCode}");
                return true;
            }
            case TweakAction.Wsl:
                _log.Write("  NOTE: WSL2 reversal is not automatic. To disable the optional feature, run as Administrator:");
                _log.Write("        dism /online /disable-feature /featurename:Microsoft-Windows-Subsystem-Linux /norestart");
                return false;
            case TweakAction.DevMode:
                SetMachineDword(DevModeSubKey, "AllowDevelopmentWithoutDevLicense", 0);
                return true;
            case TweakAction.LongPath:
                SetMachineDword(LongPathSubKey, "LongPathsEnabled", 0);
                return true;
            default:
                return false;
        }
    }

    private static void SetMachineDword(string subKey, string name, int value)
    {
        try
        {
            using var key = Registry.LocalMachine.CreateSubKey(subKey, writable: true)
                ?? throw new InvalidOperationException("could not open the registry key");
            key.SetValue(name, value, RegistryValueKind.DWord);
        }
        catch (Exception ex) when (ex is UnauthorizedAccessException or SecurityException)
        {
            throw new InvalidOperationException("registry write failed - try running as Administrator");
        }
    }

    private static string FailureReason(RunResult r, string fallback)
    {
        if (!r.Started) return r.Stderr.Trim();
        var tail = r.Tail();
        return tail.Length > 0 ? tail : fallback;
    }
}
