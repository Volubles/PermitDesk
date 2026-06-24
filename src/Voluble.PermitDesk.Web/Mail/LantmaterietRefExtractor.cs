using System.Text.RegularExpressions;

namespace Voluble.PermitDesk.Web.Mail;

public static partial class LantmaterietRefExtractor
{
    [GeneratedRegex(@"LM\d{4}/\d{6}", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex Pattern();

    /// <summary>
    /// Extracts Lantmäteriet case references in the format LMYYYY/NNNNNN
    /// (e.g. "LM2026/093031"). Upper-cased, deduplicated, in order of first occurrence.
    /// Safe with null/empty input.
    /// </summary>
    public static IReadOnlyList<string> Extract(string? text)
    {
        if (string.IsNullOrEmpty(text)) return Array.Empty<string>();

        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var ordered = new List<string>();
        foreach (Match m in Pattern().Matches(text))
        {
            var normalised = m.Value.ToUpperInvariant();
            if (seen.Add(normalised))
                ordered.Add(normalised);
        }
        return ordered;
    }
}
