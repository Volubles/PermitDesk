using Microsoft.EntityFrameworkCore;
using Voluble.PermitDesk.Web.Data;

namespace Voluble.PermitDesk.Web.Cases;

public class CaseNumberGenerator(IDbContextFactory<PermitDeskDbContext> dbFactory)
{
    public static string Format(int year, int sequence)
        => $"FBO-{year:0000}-{sequence:0000}";

    public async Task<string> GenerateNextAsync(DateTimeOffset now, CancellationToken ct = default)
    {
        var year = now.Year;
        var prefix = Format(year, 0)[..^4];

        await using var db = await dbFactory.CreateDbContextAsync(ct);

        var existingNumbers = await db.Cases
            .Where(c => EF.Functions.Like(c.CaseNumber, prefix + "%"))
            .Select(c => c.CaseNumber)
            .ToListAsync(ct);

        var maxSequence = existingNumbers
            .Select(n => int.TryParse(n.AsSpan(prefix.Length), out var v) ? v : 0)
            .DefaultIfEmpty(0)
            .Max();

        return Format(year, maxSequence + 1);
    }
}
