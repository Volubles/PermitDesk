using System.Net;
using System.Text.RegularExpressions;

namespace Voluble.PermitDesk.Web.Mail;

/// <summary>
/// Minimal HTML-to-text converter used when an inbound mail is pure HTML with
/// no plain-text alternative (Lantmäteriet's mails are this shape). Goal is
/// "readable enough for classifier substring matches and PDF rendering" —
/// not pixel-perfect formatting.
/// </summary>
public static partial class HtmlToText
{
    [GeneratedRegex(@"<br\s*/?>", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex BreakTag();

    [GeneratedRegex(@"</?(p|div|tr|h\d|li|table)[^>]*>", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex BlockTag();

    [GeneratedRegex(@"<style[^>]*>.*?</style>|<script[^>]*>.*?</script>",
        RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.CultureInvariant)]
    private static partial Regex StyleOrScript();

    [GeneratedRegex(@"<!--.*?-->",
        RegexOptions.Singleline | RegexOptions.CultureInvariant)]
    private static partial Regex HtmlComment();

    [GeneratedRegex(@"<[^>]+>", RegexOptions.CultureInvariant)]
    private static partial Regex AnyTag();

    [GeneratedRegex(@"[ \t]+", RegexOptions.CultureInvariant)]
    private static partial Regex Spaces();

    [GeneratedRegex(@"\n[ \t]*(\n[ \t]*)+", RegexOptions.CultureInvariant)]
    private static partial Regex BlankLines();

    public static string Convert(string? html)
    {
        if (string.IsNullOrEmpty(html)) return string.Empty;

        var s = StyleOrScript().Replace(html, " ");
        s = HtmlComment().Replace(s, " ");
        s = BreakTag().Replace(s, "\n");
        s = BlockTag().Replace(s, "\n");
        s = AnyTag().Replace(s, "");
        s = WebUtility.HtmlDecode(s);
        s = Spaces().Replace(s, " ");
        s = BlankLines().Replace(s, "\n\n");
        return s.Trim();
    }
}
