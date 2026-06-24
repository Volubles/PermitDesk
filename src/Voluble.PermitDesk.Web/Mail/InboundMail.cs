namespace Voluble.PermitDesk.Web.Mail;

public record InboundMail(
    string MessageId,
    string From,
    string Subject,
    string TextBody,
    string? HtmlBody,
    DateTimeOffset ReceivedAt,
    string SourcePath);
