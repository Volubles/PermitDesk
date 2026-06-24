using System.Text.RegularExpressions;

namespace Voluble.PermitDesk.Web.Mail;

public static partial class UploadLinkExtractor
{
    [GeneratedRegex(
        @"https://filskick-spridningstillstand\.lantmateriet\.se/skicka/[A-Za-z0-9_\-]+",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex Pattern();

    /// <summary>
    /// Extracts Lantmäteriet filskick upload URLs. URLs are case-sensitive (the
    /// token after /skicka/ is base64-ish), so output preserves original casing.
    /// Deduplicated in order of first occurrence — the same URL appearing in
    /// both an &lt;a href&gt; and visible link text only counts once.
    /// </summary>
    public static IReadOnlyList<string> Extract(string? text)
    {
        if (string.IsNullOrEmpty(text)) return Array.Empty<string>();

        var seen = new HashSet<string>(StringComparer.Ordinal);
        var ordered = new List<string>();
        foreach (Match m in Pattern().Matches(text))
        {
            if (seen.Add(m.Value))
                ordered.Add(m.Value);
        }
        return ordered;
    }
}
