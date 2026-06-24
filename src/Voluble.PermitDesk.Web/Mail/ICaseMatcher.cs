namespace Voluble.PermitDesk.Web.Mail;

public interface ICaseMatcher
{
    Task<MatchResult> MatchAsync(InboundMail mail, CancellationToken ct = default);
}
