using Voluble.PermitDesk.Web.Mail;

namespace Voluble.PermitDesk.Tests;

public class CaseReferenceExtractorTests
{
    [Theory]
    [InlineData("FBO-2026-0001")]
    [InlineData("Bekräftelse FBO-2026-0001 - Storgatan 4")]
    [InlineData("Vår referens: FBO-2026-0001.")]
    [InlineData("(FBO-2026-0001)")]
    public void Extracts_single_reference(string text)
    {
        var refs = CaseReferenceExtractor.Extract(text);
        Assert.Single(refs);
        Assert.Equal("FBO-2026-0001", refs[0]);
    }

    [Fact]
    public void Extracts_multiple_references_in_order_without_duplicates()
    {
        var text = "Två ärenden i samma mejl: FBO-2026-0002 och FBO-2026-0001. " +
                   "Påminnelse: FBO-2026-0001 igen.";
        var refs = CaseReferenceExtractor.Extract(text);

        Assert.Equal(new[] { "FBO-2026-0002", "FBO-2026-0001" }, refs);
    }

    [Fact]
    public void Match_is_case_insensitive_but_output_is_uppercase()
    {
        var refs = CaseReferenceExtractor.Extract("re: fbo-2026-0001 from broker");
        Assert.Equal("FBO-2026-0001", Assert.Single(refs));
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    [InlineData("Inget ärende här.")]
    [InlineData("FBO-26-0001")]      // year too short
    [InlineData("FBO-2026-1")]       // sequence too short
    [InlineData("FBO-20260-0001")]   // year too long
    [InlineData("ABC-2026-0001")]    // wrong prefix
    public void Returns_empty_for_no_match(string? text)
    {
        Assert.Empty(CaseReferenceExtractor.Extract(text));
    }
}
