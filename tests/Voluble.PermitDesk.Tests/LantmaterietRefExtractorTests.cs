using Voluble.PermitDesk.Web.Mail;

namespace Voluble.PermitDesk.Tests;

public class LantmaterietRefExtractorTests
{
    [Theory]
    [InlineData("Vi har registrerat er ansökan om spridningstillstånd (LM2026/093031)")]
    [InlineData("Ansökan har fått ärendenummer: LM2026/093031, uppge detta vid kontakter")]
    [InlineData("mailto:spridningstillstand@lm.se?subject=LM2026/093031:")]
    [InlineData("LM2026/093031")]
    public void Extracts_single_reference(string text)
    {
        var refs = LantmaterietRefExtractor.Extract(text);
        Assert.Single(refs);
        Assert.Equal("LM2026/093031", refs[0]);
    }

    [Fact]
    public void Deduplicates_when_same_ref_appears_multiple_times()
    {
        // Real Lantmäteriet mail includes the ref in subject, body intro, and mailto link.
        var refs = LantmaterietRefExtractor.Extract(
            "Subject: (LM2026/093031) ... ärendenummer: LM2026/093031 ... LM2026/093031");
        Assert.Single(refs);
    }

    [Fact]
    public void Extracts_multiple_distinct_references_in_order()
    {
        var refs = LantmaterietRefExtractor.Extract(
            "Ärenden i samma mejl: LM2026/093031 och LM2025/000042 + LM2026/093031");
        Assert.Equal(["LM2026/093031", "LM2025/000042"], refs);
    }

    [Fact]
    public void Case_insensitive_match_normalises_output_to_uppercase()
    {
        var refs = LantmaterietRefExtractor.Extract("re: lm2026/093031 from lm.se");
        Assert.Equal("LM2026/093031", Assert.Single(refs));
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    [InlineData("Inget ärende här.")]
    [InlineData("LM2026/12345")]     // sequence too short
    [InlineData("LM26/093031")]      // year too short
    [InlineData("LM2026093031")]     // missing slash
    [InlineData("XM2026/093031")]    // wrong prefix
    public void Returns_empty_for_no_match(string? text)
    {
        Assert.Empty(LantmaterietRefExtractor.Extract(text));
    }
}
