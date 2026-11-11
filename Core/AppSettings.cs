using System.Text.Json;

namespace AiSetupUi;

/// <summary>Small user settings saved as settings.json next to the exe.</summary>
internal sealed class AppSettings
{
    // ---- theme & news ----
    public string Theme { get; set; } = "Light";
    public string NewsQuery { get; set; } = "";
    public int NewsDays { get; set; } = 30;
    public bool NewsPythonOnly { get; set; } = true;

    // ---- window geometry (0 = "use the default") ----
    public int WindowWidth { get; set; }
    public int WindowHeight { get; set; }
    public int TreeSplit { get; set; }

    private static string FilePath => Path.Combine(AppPaths.Root, "settings.json");

    public static AppSettings Load()
    {
        AppSettings s;
        try
        {
            if (File.Exists(FilePath))
                s = JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(FilePath)) ?? new AppSettings();
            else
                s = new AppSettings();
        }
        catch
        {
            // corrupt or unreadable settings: fall back to defaults
            s = new AppSettings();
        }

        s.Normalize();
        return s;
    }

    public void Save()
    {
        Normalize();
        try
        {
            var json = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
            var tmp = FilePath + ".tmp";
            File.WriteAllText(tmp, json);
            // Atomic replace so a crash mid-write can't leave a half-written file.
            if (File.Exists(FilePath))
                File.Replace(tmp, FilePath, null);
            else
                File.Move(tmp, FilePath);
        }
        catch
        {
            // settings are best-effort - never crash the app over them
        }
    }

    /// <summary>Keep values inside sane bounds so a hand-edited settings.json can't break the UI.</summary>
    private void Normalize()
    {
        if (string.IsNullOrWhiteSpace(Theme)) Theme = "Light";
        if (NewsDays is not (7 or 30 or 90)) NewsDays = 30;

        if (WindowWidth  is < 800 or > 6000) WindowWidth  = 0;  // 0 = default
        if (WindowHeight is < 700 or > 6000) WindowHeight = 0;
        if (TreeSplit    is < 150 or > 4000) TreeSplit    = 0;
    }
}