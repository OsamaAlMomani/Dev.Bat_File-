using System.Text.Json;

namespace AiSetupUi;

internal sealed record PipPackage(string Name, string Version);

internal sealed record PipOutdated(string Name, string Version, string Latest);

internal static class PipService
{
    /// <summary>Returns null when pip can't be run or its output can't be read.</summary>
    public static async Task<List<PipPackage>?> ListAsync(CancellationToken ct)
    {
        var r = await ProcessRunner.RunAsync(AppPaths.PythonExe,
            new[] { "-m", "pip", "list", "--format=json" }, ct);
        if (!r.Started || r.ExitCode != 0 || string.IsNullOrWhiteSpace(r.Stdout)) return null;

        try
        {
            using var doc = JsonDocument.Parse(r.Stdout);
            var list = new List<PipPackage>();
            foreach (var el in doc.RootElement.EnumerateArray())
            {
                var name = el.GetProperty("name").GetString();
                var version = el.TryGetProperty("version", out var v) ? v.GetString() : "";
                if (!string.IsNullOrEmpty(name)) list.Add(new PipPackage(name, version ?? ""));
            }
            return list;
        }
        catch (Exception ex) when (ex is JsonException or InvalidOperationException or KeyNotFoundException)
        {
            return null;
        }
    }

    /// <summary>Returns null when PyPI couldn't be queried.</summary>
    public static async Task<List<PipOutdated>?> ListOutdatedAsync(CancellationToken ct)
    {
        var r = await ProcessRunner.RunAsync(AppPaths.PythonExe,
            new[] { "-m", "pip", "list", "--outdated", "--format=json" }, ct);
        if (!r.Started || r.ExitCode != 0 || string.IsNullOrWhiteSpace(r.Stdout)) return null;

        try
        {
            using var doc = JsonDocument.Parse(r.Stdout);
            var list = new List<PipOutdated>();
            foreach (var el in doc.RootElement.EnumerateArray())
            {
                var name = el.GetProperty("name").GetString() ?? "";
                var version = el.TryGetProperty("version", out var v) ? v.GetString() ?? "" : "";
                var latest = el.TryGetProperty("latest_version", out var l) ? l.GetString() ?? "" : "";
                if (name.Length > 0) list.Add(new PipOutdated(name, version, latest));
            }
            return list;
        }
        catch (Exception ex) when (ex is JsonException or InvalidOperationException or KeyNotFoundException)
        {
            return null;
        }
    }
}
