using Voluble.PermitDesk.Web.Mail;

namespace Voluble.PermitDesk.Tests;

public class UploadLinkExtractorTests
{
    private const string RealUrl =
        "https://filskick-spridningstillstand.lantmateriet.se/skicka/AmvvxgZAiG4TrRX8Rj9Uw3X7yvRPVRzbMiVUbAHXd0g";

    [Fact]
    public void Extracts_url_from_plain_text()
    {
        var urls = UploadLinkExtractor.Extract($"Vänligen ladda upp materialet via {RealUrl} inom 14 dagar.");
        Assert.Equal(RealUrl, Assert.Single(urls));
    }

    [Fact]
    public void Extracts_url_from_html_anchor()
    {
        // Real mail has the URL twice — as href and as visible text inside the anchor.
        var html = $"""<a href="{RealUrl}">{RealUrl}</a>""";
        var urls = UploadLinkExtractor.Extract(html);
        Assert.Equal(RealUrl, Assert.Single(urls));
    }

    [Fact]
    public void Preserves_url_token_case()
    {
        // Token after /skicka/ is case-sensitive. Verify we don't accidentally lowercase it.
        var url = "https://filskick-spridningstillstand.lantmateriet.se/skicka/MiXeDcaseTokenABC123_-xyz";
        var urls = UploadLinkExtractor.Extract($"link: {url} end");
        Assert.Equal(url, Assert.Single(urls));
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    [InlineData("https://www.lantmateriet.se/sv/spridningstillstand/vanliga-fragor/")]
    [InlineData("https://filskick.lantmateriet.se/skicka/abc")]      // wrong host
    [InlineData("http://filskick-spridningstillstand.lantmateriet.se/skicka/abc")] // wrong scheme
    public void Returns_empty_for_no_match(string? text)
    {
        Assert.Empty(UploadLinkExtractor.Extract(text));
    }

    [Fact]
    public void Extracts_multiple_distinct_urls_in_order()
    {
        var a = "https://filskick-spridningstillstand.lantmateriet.se/skicka/aaaaaaaaaaaa";
        var b = "https://filskick-spridningstillstand.lantmateriet.se/skicka/bbbbbbbbbbbb";
        var urls = UploadLinkExtractor.Extract($"first {a} then {b} and again {a}");
        Assert.Equal([a, b], urls);
    }
}
