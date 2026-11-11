using Microsoft.Win32;

namespace AiSetupUi;

/// <summary>What the machine looks like right now. Read-only, never changes anything.</summary>
internal sealed class StatusSnapshot
{
    public HashSet<string>? WingetTokens { get; init; }
    public HashSet<string> Pip { get; init; } = new();
    public bool? WslInstalled { get; init; }

    public const string DevModeKey = @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\AppModelUnlock";
    public const string LongPathKey = @"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\FileSystem";

    /// <summary>true = installed, false = not installed, null = couldn't tell.</summary>
    public bool? Evaluate(SetupItem item) => item.Type switch
    {
        ItemType.Winget => WingetTokens?.Contains(item.Id),
        ItemType.Pip => Pip.Contains(PipName.Normalize(item.Id)),
        ItemType.Tweak => EvaluateTweak(item.Tweak!),
        ItemType.Project => EvaluateProject(item.Id),
        _ => null,
    };

    private bool? EvaluateTweak(Tweak tweak) => tweak.Action switch
    {
        TweakAction.Winget => WingetTokens?.Contains(tweak.WingetId!),
        TweakAction.Wsl => WslInstalled,
        TweakAction.DevMode => RegistryDwordEquals(DevModeKey, "AllowDevelopmentWithoutDevLicense", 1),
        TweakAction.LongPath => RegistryDwordEquals(LongPathKey, "LongPathsEnabled", 1),
        _ => null,
    };

    private static bool? EvaluateProject(string id) => id switch
    {
        "VENV" => File.Exists(AppPaths.VenvPython),
        "DIRS" => Directory.Exists(AppPaths.ProjectsDir),
        "FREEZE" => File.Exists(AppPaths.InstalledPackagesFile) && File.Exists(AppPaths.RequirementsFile),
        "IPYKERNEL" => Directory.Exists(AppPaths.KernelDir),
        _ => null,
    };

    private static bool RegistryDwordEquals(string path, string name, int expected)
    {
        try { return Registry.GetValue(path, name, null) is int v && v == expected; }
        catch { return false; }
    }
}

internal static class StatusService
{
    public static async Task<StatusSnapshot> ScanAsync(Logger log, CancellationToken ct)
    {
        // winget: one "winget list" call, then match IDs against whitespace-separated tokens.
        HashSet<string>? wingetTokens = null;
        var list = await ProcessRunner.RunAsync("winget",
            new[] { "list", "--accept-source-agreements" }, ct);
        if (list.Started && !string.IsNullOrWhiteSpace(list.Stdout))
        {
            wingetTokens = new HashSet<string>(
                list.Stdout.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries),
                StringComparer.OrdinalIgnoreCase);
        }
        else
        {
            log.Write("  NOTE: could not run winget list - winget items will show as unknown.");
        }

        var pipSet = new HashSet<string>();
        var pip = await PipService.ListAsync(ct);
        if (pip != null)
            foreach (var p in pip) pipSet.Add(PipName.Normalize(p.Name));

        var wsl = await ProcessRunner.RunAsync("wsl", new[] { "--status" }, ct);

        return new StatusSnapshot
        {
            WingetTokens = wingetTokens,
            Pip = pipSet,
            WslInstalled = wsl.Started && wsl.ExitCode == 0,
        };
    }
}
