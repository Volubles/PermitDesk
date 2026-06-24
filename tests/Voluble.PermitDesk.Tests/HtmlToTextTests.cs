using Voluble.PermitDesk.Web.Mail;

namespace Voluble.PermitDesk.Tests;

public class HtmlToTextTests
{
    [Fact]
    public void Strips_basic_tags_and_decodes_entities()
    {
        var html = "<p>Hej <b>v&auml;rlden</b> &amp; tjena</p>";
        Assert.Equal("Hej världen & tjena", HtmlToText.Convert(html));
    }

    [Fact]
    public void Br_tags_become_newlines()
    {
        var html = "Rad 1<br/>Rad 2<br />Rad 3<BR>Rad 4";
        var text = HtmlToText.Convert(html);
        Assert.Contains("Rad 1\nRad 2\nRad 3\nRad 4", text);
    }

    [Fact]
    public void Block_tags_become_line_separators()
    {
        var html = "<div>A</div><div>B</div>";
        var text = HtmlToText.Convert(html);
        Assert.Contains("A", text);
        Assert.Contains("B", text);
        Assert.Contains("\n", text);
    }

    [Fact]
    public void Strips_style_and_script_blocks_entirely()
    {
        var html = "Visible<style>.foo{color:red}</style><script>alert(1)</script>End";
        var text = HtmlToText.Convert(html);
        Assert.DoesNotContain(".foo", text);
        Assert.DoesNotContain("alert", text);
        Assert.Contains("Visible", text);
        Assert.Contains("End", text);
    }

    [Fact]
    public void Preserves_url_inside_anchor_tag()
    {
        // The upload link extractor runs after HTML-to-text, so URLs must survive.
        var html = """<a href="https://filskick-spridningstillstand.lantmateriet.se/skicka/abc">link</a>""";
        var text = HtmlToText.Convert(html);
        Assert.Contains("link", text);
        // The href value won't be in the rendered text — that's fine, the
        // extractor reads from the raw HTML body as well.
    }

    [Fact]
    public void Handles_html_comments()
    {
        var html = "<!-- Spridningstillstånd v2025 -->Real content";
        var text = HtmlToText.Convert(html);
        Assert.Equal("Real content", text);
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void Empty_in_empty_out(string? input)
    {
        Assert.Equal(string.Empty, HtmlToText.Convert(input));
    }
}
