using Voluble.PermitDesk.Web.Domain;

namespace Voluble.PermitDesk.Web.Mail;

public record MatchResult(Guid? CaseId, MailMatchConfidence Confidence);
