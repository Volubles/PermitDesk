using System.Text.RegularExpressions;

namespace Voluble.PermitDesk.Web.Mail;

public static partial class CaseReferenceExtractor
{
    [GeneratedRegex(@"FBO-\d{4}-\d{4}", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex FboPattern();

    /// <summary>
    /// Extracts every FBO-YYYY-NNNN reference found in the input text. Returns
    /// upper-cased, deduplicated values in the order of first occurrence.
    /// Safe to call with null or empty input.
    /// </summary>
    public static IReadOnlyList<string> Extract(string? text)
    {
        if (string.IsNullOrEmpty(text)) return Array.Empty<string>();

        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var ordered = new List<string>();

        foreach (Match m in FboPattern().Matches(text))
        {
            var normalised = m.Value.ToUpperInvariant();
            if (seen.Add(normalised))
                ordered.Add(normalised);
        }

        return ordered;
    }
}
