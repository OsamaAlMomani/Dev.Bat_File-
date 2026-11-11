using System.Text;

namespace AiSetupUi;

/// <summary>Writes to setup_log.txt and echoes each line to the window's log box.</summary>
internal sealed class Logger
{
    private static readonly UTF8Encoding Utf8NoBom = new(false);
    private readonly Action<string> _echo;

    public Logger(Action<string> echo) => _echo = echo;

    /// <summary>Starts a fresh log file (same as the old script: each run overwrites the last).</summary>
    public void StartSection(string title, int selectedCount)
    {
        var sb = new StringBuilder();
        sb.AppendLine("============================================================");
        sb.AppendLine(title);
        sb.AppendLine($"Started: {DateTime.Now}");
        sb.AppendLine($"Selected items: {selectedCount}");
        sb.AppendLine("============================================================");
        try { File.WriteAllText(AppPaths.LogFile, sb.ToString(), Utf8NoBom); } catch { /* log is best-effort */ }
    }

    public void Write(string message)
    {
        var line = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}";
        try { File.AppendAllText(AppPaths.LogFile, line + Environment.NewLine, Utf8NoBom); } catch { }
        _echo(line);
    }
}
