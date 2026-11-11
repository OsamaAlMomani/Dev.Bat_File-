using System.Text.RegularExpressions;

namespace AiSetupUi;

internal static class PipName
{
    /// <summary>Lower-cases, strips extras like [cli], and treats - _ . as the same character.</summary>
    public static string Normalize(string raw)
    {
        var n = raw.Trim().Trim('"', '\'');
        var bracket = n.IndexOf('[');
        if (bracket >= 0) n = n[..bracket];
        return Regex.Replace(n, @"[-_.]+", "-").ToLowerInvariant();
    }
}
