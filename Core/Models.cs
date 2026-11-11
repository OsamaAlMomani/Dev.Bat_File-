namespace AiSetupUi;

internal enum ItemType { Winget, Tweak, Pip, Project }

internal enum TweakAction { Winget, Wsl, DevMode, LongPath }

internal sealed record WingetPackage(string Id, string Name);

internal sealed record Tweak(string Id, string Name, TweakAction Action, string? WingetId = null);

internal sealed record ProjectOption(string Id, string Name);

/// <summary>One checkable thing: a winget app, a tweak, a pip package or a project step.</summary>
internal sealed class SetupItem
{
    public required ItemType Type { get; init; }
    public required string Id { get; init; }
    public required string Name { get; init; }
    public Tweak? Tweak { get; init; }

    /// <summary>True for pip packages found by "Scan System Extras" (already installed, not in the catalog).</summary>
    public bool IsExtra { get; init; }
    public string? Version { get; init; }
}

internal sealed class Tally
{
    public int Ok;
    public int Fail;
    public int Skip;
    public List<string> Failed { get; } = new();

    public void Bad(string name)
    {
        Fail++;
        Failed.Add(name);
    }
}
