using Microsoft.EntityFrameworkCore;
using Voluble.PermitDesk.Web.Data;
using Voluble.PermitDesk.Web.Domain;

namespace Voluble.PermitDesk.Web.Mail;

public class CaseMatcher(IDbContextFactory<PermitDeskDbContext> dbFactory) : ICaseMatcher
{
    public async Task<MatchResult> MatchAsync(InboundMail mail, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);

        // Primary: FBO reference in subject or body.
        var combined = $"{mail.Subject}\n{mail.TextBody}";
        var refs = CaseReferenceExtractor.Extract(combined);
        if (refs.Count > 0)
        {
            var match = await db.Cases
                .AsNoTracking()
                .Where(c => refs.Contains(c.CaseNumber))
                .Select(c => new { c.Id })
                .FirstOrDefaultAsync(ct);

            if (match is not null)
                return new MatchResult(match.Id, MailMatchConfidence.HighReference);
        }

        // Secondary: Lantmäteriet ärendenummer (LMYYYY/NNNNNN). Set on the case
        // after the first UploadLinkReceived mail; subsequent decision mails
        // sometimes carry only this ref, not the broker's FBO ref.
        var lmRefs = LantmaterietRefExtractor.Extract(combined);
        if (lmRefs.Count > 0)
        {
            var match = await db.Cases
                .AsNoTracking()
                .Where(c => c.LantmaterietReferenceNumber != null
                         && lmRefs.Contains(c.LantmaterietReferenceNumber))
                .Select(c => new { c.Id })
                .FirstOrDefaultAsync(ct);

            if (match is not null)
                return new MatchResult(match.Id, MailMatchConfidence.HighReference);
        }

        // Fallback: address substring match.
        var haystack = combined.ToLowerInvariant();
        var addresses = await db.Cases
            .AsNoTracking()
            .Select(c => new { c.Id, c.Address })
            .ToListAsync(ct);

        foreach (var c in addresses)
        {
            if (!string.IsNullOrWhiteSpace(c.Address)
                && haystack.Contains(c.Address.ToLowerInvariant()))
            {
                return new MatchResult(c.Id, MailMatchConfidence.MediumAddress);
            }
        }

        return new MatchResult(null, MailMatchConfidence.NoMatch);
    }
}
