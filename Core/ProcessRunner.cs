using System.ComponentModel;
using System.Diagnostics;
using System.Text;

namespace AiSetupUi;

internal sealed record RunResult(int ExitCode, string Stdout, string Stderr, bool Started)
{
    public string Combined => (Stdout + Environment.NewLine + Stderr).Trim();

    /// <summary>Last few meaningful lines, handy for explaining a failure in the log.</summary>
    public string Tail(int lines = 3)
    {
        var picked = Combined
            .Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .Select(l => l.Trim())
            .Where(l => l.Length > 2 && l.Any(char.IsLetterOrDigit))
            .TakeLast(lines);
        return string.Join(" | ", picked);
    }
}

internal static class ProcessRunner
{
    /// <summary>
    /// Runs a program without a console window and waits for it. If the token is cancelled
    /// the whole process tree is killed and OperationCanceledException is thrown.
    /// </summary>
    public static async Task<RunResult> RunAsync(string file, IEnumerable<string> args, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        var psi = new ProcessStartInfo(file)
        {
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8,
            WorkingDirectory = AppPaths.Root,
        };
        foreach (var a in args) psi.ArgumentList.Add(a);
        psi.Environment["PYTHONIOENCODING"] = "utf-8";

        var stdout = new StringBuilder();
        var stderr = new StringBuilder();
        var gate = new object();

        using var proc = new Process { StartInfo = psi };
        proc.OutputDataReceived += (_, e) => { if (e.Data != null) lock (gate) stdout.AppendLine(e.Data); };
        proc.ErrorDataReceived += (_, e) => { if (e.Data != null) lock (gate) stderr.AppendLine(e.Data); };

        try
        {
            proc.Start();
        }
        catch (Win32Exception ex)
        {
            return new RunResult(-1, "", ex.Message, false);
        }

        proc.BeginOutputReadLine();
        proc.BeginErrorReadLine();

        using var registration = ct.Register(() =>
        {
            try { if (!proc.HasExited) proc.Kill(entireProcessTree: true); } catch { }
        });

        await proc.WaitForExitAsync(CancellationToken.None);
        ct.ThrowIfCancellationRequested();

        string o, e2;
        lock (gate) { o = stdout.ToString(); e2 = stderr.ToString(); }
        return new RunResult(proc.ExitCode, o, e2, true);
    }

    /// <summary>True when the program starts and "--version" style probe exits with 0.</summary>
    public static async Task<bool> IsAvailableAsync(string file, string probeArg, CancellationToken ct)
    {
        var r = await RunAsync(file, new[] { probeArg }, ct);
        return r.Started && r.ExitCode == 0;
    }
}
