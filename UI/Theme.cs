namespace AiSetupUi;

/// <summary>How a control wants to be coloured. Set it in Control.Tag; the theme does the rest.</summary>
internal enum ThemeRole
{
    Default, Muted, Primary, Danger, Secondary, Selected,
    Separator, WarnText, WarnBanner,
}

/// <summary>
/// Every colour the app uses lives here. To add a theme, add one more block to <see cref="Themes"/>.
/// No other file should contain a Color literal.
/// </summary>
internal sealed class Theme
{
    public required string Name { get; init; }
    public required bool IsDark { get; init; }

    public required Color Window { get; init; }     // form + panel background
    public required Color Surface { get; init; }    // text boxes, tree, lists
    public required Color Text { get; init; }
    public required Color Muted { get; init; }      // hints, status, unknown items
    public required Color Accent { get; init; }     // root nodes in the tree
    public required Color Category { get; init; }   // category nodes in the tree
    public required Color Good { get; init; }       // installed / available
    public required Color Link { get; init; }       // "extra" packages
    public required Color Match { get; init; }      // filter highlight
    public required Color Border { get; init; }
    public required Color Primary { get; init; }    // Install button
    public required Color Danger { get; init; }     // Uninstall button
    public required Color Neutral { get; init; }    // normal buttons, list headers, status strip
    public required Color Selected { get; init; }   // active tab button
    public required Color ButtonText { get; init; }
    public required Color Warn { get; init; }       // warning text (also in log)
    public required Color WarnBg { get; init; }     // admin banner background

    /// <summary>Text colour for one tree node, based on what it is and whether it is installed.</summary>
    public Color NodeColor(NodeKind kind, bool? installed, bool statusKnown, bool isExtra) => kind switch
    {
        NodeKind.Root or NodeKind.ExtrasRoot => Accent,
        NodeKind.Category => Category,
        NodeKind.Info => Muted,
        _ => isExtra ? Link
           : !statusKnown ? Text
           : installed switch { true => Good, false => Text, null => Muted },
    };
}

internal static class Themes
{
    public static readonly Theme Light = new()
    {
        Name = "Light", IsDark = false,
        Window = Color.FromArgb(243, 243, 243), Surface = Color.White,
        Text = Color.FromArgb(30, 30, 30), Muted = Color.DimGray,
        Accent = Color.FromArgb(20, 50, 110), Category = Color.FromArgb(50, 60, 80),
        Good = Color.Green, Link = Color.Blue, Match = Color.LightYellow,
        Border = Color.FromArgb(180, 180, 180),
        Primary = Color.LightGreen, Danger = Color.LightCoral,
        Neutral = Color.FromArgb(225, 225, 225), Selected = Color.FromArgb(190, 215, 255),
        ButtonText = Color.Black,
        Warn = Color.FromArgb(140, 90, 0), WarnBg = Color.FromArgb(255, 243, 205),
    };

    public static readonly Theme Dark = new()
    {
        Name = "Dark", IsDark = true,
        Window = Color.FromArgb(30, 30, 34), Surface = Color.FromArgb(40, 40, 46),
        Text = Color.FromArgb(230, 230, 230), Muted = Color.FromArgb(150, 150, 162),
        Accent = Color.FromArgb(130, 170, 255), Category = Color.FromArgb(175, 185, 205),
        Good = Color.FromArgb(110, 200, 120), Link = Color.FromArgb(120, 170, 255),
        Match = Color.FromArgb(95, 85, 20), Border = Color.FromArgb(75, 75, 84),
        Primary = Color.FromArgb(40, 110, 60), Danger = Color.FromArgb(150, 55, 55),
        Neutral = Color.FromArgb(58, 58, 66), Selected = Color.FromArgb(60, 85, 140),
        ButtonText = Color.White,
        Warn = Color.FromArgb(240, 190, 80), WarnBg = Color.FromArgb(70, 55, 20),
    };

    public static readonly IReadOnlyList<Theme> All = new[] { Light, Dark };

    public static Theme ByName(string? name) =>
        All.FirstOrDefault(t => t.Name.Equals(name, StringComparison.OrdinalIgnoreCase)) ?? Light;
}