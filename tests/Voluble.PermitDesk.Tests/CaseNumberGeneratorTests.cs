using Voluble.PermitDesk.Web.Cases;

namespace Voluble.PermitDesk.Tests;

public class CaseNumberGeneratorTests
{
    [Theory]
    [InlineData(2026, 1, "FBO-2026-0001")]
    [InlineData(2026, 42, "FBO-2026-0042")]
    [InlineData(2026, 9999, "FBO-2026-9999")]
    [InlineData(2027, 1, "FBO-2027-0001")]
    public void Format_pads_sequence_to_four_digits(int year, int sequence, string expected)
    {
        Assert.Equal(expected, CaseNumberGenerator.Format(year, sequence));
    }

    [Fact]
    public void Format_handles_year_rollover()
    {
        // New year resets the sequence — verified by formatting, the DB query
        // does the lookup-by-year-prefix.
        var lastOf2026 = CaseNumberGenerator.Format(2026, 9999);
        var firstOf2027 = CaseNumberGenerator.Format(2027, 1);

        Assert.Equal("FBO-2026-9999", lastOf2026);
        Assert.Equal("FBO-2027-0001", firstOf2027);
    }
}
